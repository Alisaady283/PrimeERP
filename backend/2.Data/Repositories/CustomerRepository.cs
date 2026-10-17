using System;
using System.Collections.Generic;
using PrimeERP.Data.Core;
using PrimeERP.Data.Repositories.Base;
using PrimeERP.Domain.Entities;

namespace PrimeERP.Data.Repositories
{
    /// <summary>طبقة وصول بيانات العملاء</summary>
    public class CustomerRepository : PartyRepositoryBase<Customer>, IPartyRepository<Customer>
    {
        protected override string TableName => "Customers";

    }
}
