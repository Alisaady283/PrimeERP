using System;
using System.Collections.Generic;
using PrimeERP.Data.Core;
using PrimeERP.Data.Repositories.Base;
using PrimeERP.Domain.Entities;
using PrimeERP.Domain.Enums;

namespace PrimeERP.Data.Repositories
{
    /// <summary>طبقة وصول بيانات الموردين</summary>
    public class SupplierRepository : PartyRepositoryBase<Supplier>, IPartyRepository<Supplier>
    {
        protected override string TableName => "Suppliers";

    }
}
