"""يولّد PrimeDbContext من القاعدة الحيّة: كل جدولٍ بكيانه، وكل خاصيةٍ بلا عمود تُتجاهَل"""
import json
import re
import sys
from pathlib import Path

sys.path.insert(0, "Tools/Docs")
from model import entities

TABLES = json.load(open("Tools/Docs/schema.json", encoding="utf-8"))
BOND = json.load(open("Tools/Docs/bond.json", encoding="utf-8"))["bond"]
INDEXES = json.load(open("Tools/Docs/indexes.json", encoding="utf-8"))

# كيانٌ واحد لعدة جداول: تُعلَن بـSharedTypeEntity ويُناديها المستودع باسم جدوله
SHARED = {e for e in ("StockAdjustment", "StockAdjustmentLine", "CycleDocument", "CycleDocumentLine")}

RENAME = {("Asset", "DepreciationAccountCode"): "DepAccountCode",
          ("Category", "DepreciationAccountCode"): "DepAccountCode",
          ("Attendance", "CheckIn"): "CheckInMinutes",
          ("Attendance", "CheckOut"): "CheckOutMinutes"}

MINUTES = {("Attendance", "CheckIn"), ("Attendance", "CheckOut")}

# خاصيةٌ محسوبة أو مملوءة بضمّة: تُتجاهَل صراحةً. وما عداها عمودٌ يُنشئه SchemaSync إن نقص
DERIVED = {"Lines", "Allocations", "CategoryName", "BrandName", "EmployeeName", "EmployeeCode",
           "UnitName", "RoleName", "JobTitleName", "DepartmentName", "CurrentStock"}


def build():
    ents = entities()
    single = {t: e for t, e in BOND.items() if e not in SHARED}
    shared = {t: e for t, e in BOND.items() if e in SHARED}

    out = ["using System;",
           "using Microsoft.EntityFrameworkCore;",
           "using PrimeERP.Domain.Entities;",
           "using PrimeERP.Domain.Entities.Common;",
           "",
           "namespace PrimeERP.Data.Core",
           "{",
           "    /// <summary>نموذج EF مولَّد</summary>",
           "    public partial class PrimeDbContext : DbContext",
           "    {",
           "        public PrimeDbContext(DbContextOptions<PrimeDbContext> options) : base(options) { }",
           ""]

    for table, entity in sorted(single.items(), key=lambda kv: kv[1]):
        out.append(f"        public DbSet<{entity}> {table} => Set<{entity}>();")

    out += ["",
            "        protected override void OnModelCreating(ModelBuilder model)",
            "        {"]

    for table, entity in sorted(single.items()):
        out += emit(table, entity, ents, shared_type=False)
    for table, entity in sorted(shared.items()):
        out += emit(table, entity, ents, shared_type=True)

    out += ['            foreach (var (table, columns) in BuiltTables.All)',
            '                BuiltTables.Shape(model, table, columns);',
            "",
            "            ModelConventions.HideDeleted(model);",
            "        }",
            "    }",
            "}"]
    return "\n".join(out) + "\n"


def emit(table, entity, ents, shared_type):
    cols = {c[0] for c in TABLES[table]}
    props = ents[entity]
    head = (f'            model.SharedTypeEntity<{entity}>("{table}", e =>'
            if shared_type else f"            model.Entity<{entity}>(e =>")
    body = [head, "            {", f'                e.ToTable("{table}");']
    if "Id" in cols and "Id" in props:
        body.append("                e.HasKey(x => x.Id);")

    for prop in sorted(props):
        column = RENAME.get((entity, prop), prop)
        if prop in DERIVED:
            body.append(f"                e.Ignore(x => x.{prop});")
            continue
        if column != prop:
            body.append(f'                e.Property(x => x.{prop}).HasColumnName("{column}");')
        if (entity, prop) in MINUTES:
            body.append(f"                e.Property(x => x.{prop}).HasConversion("
                        "v => v == null ? (int?)null : (int)v.Value.TotalMinutes,"
                        " v => v == null ? (TimeSpan?)null : TimeSpan.FromMinutes(v.Value));")
        if props[prop].strip().startswith("decimal"):
            body.append(f"                e.Property(x => x.{prop}).HasPrecision(18, 4);")

    for index in INDEXES.get(table, []):
        if any(c not in cols for c in index["columns"]): continue
        columns = ", ".join(f'"{c}"' for c in index["columns"])
        line = f'                e.HasIndex({columns}).HasDatabaseName("{index["name"]}")'
        body.append(line + (".IsUnique();" if index["unique"] else ";"))

    body += ["            });", ""]
    return body


if __name__ == "__main__":
    target = Path("2.Data/Core/PrimeDbContext.cs")
    target.write_text(build(), encoding="utf-8")
    print(f"{target}: {len(target.read_text(encoding='utf-8').splitlines())} سطراً | "
          f"{len(BOND)} جدولاً")
