using PrimeERP.Application.Services.Entities;
using PrimeERP.Application.Validation;
using System.Linq;
using PrimeERP.Application.DTOs.Cheques;
using PrimeERP.Domain.Enums;
using PrimeERP.Domain.Results;

namespace PrimeERP.Application.Legacy.Cheques
{
    /// <summary>مستند "استلام/صرف شيكات"</summary>
    public interface IChequeDocumentService
    {
        Result<PagedResult<ChequeDto>> GetPaged(int page, int pageSize, ChequeFilter filter = null);
        Result<CreateChequeDocumentDto> GetById(int id);
        Result<ChequeDocumentResultDto> Create(CreateChequeDocumentDto dto);
        Result Update(CreateChequeDocumentDto dto);
        Result Delete(int id);
    }

    public interface IChequeReceiptDocumentService : IChequeDocumentService { }
    public interface IChequeIssueDocumentService : IChequeDocumentService { }

    public abstract class ChequeDocumentServiceBase : IChequeDocumentService
    {
        private readonly IChequeService _cheques;
        private readonly ChequeDirection _direction;

        protected ChequeDocumentServiceBase(IChequeService cheques, ChequeDirection direction)
        {
            _cheques = cheques;
            _direction = direction;
        }

        public Result<PagedResult<ChequeDto>> GetPaged(int page, int pageSize, ChequeFilter filter = null)
        {
            filter ??= new ChequeFilter();
            filter.Direction = (int)_direction;
            return _cheques.GetPaged(page, pageSize, filter);
        }

        public Result<CreateChequeDocumentDto> GetById(int id)
        {
            var cheque = _cheques.GetById(id);
            if (cheque.IsFailure) return cheque.As<CreateChequeDocumentDto>();

            return Result.Ok(Rows.Copy(cheque.Value, new CreateChequeDocumentDto
            {
                DocDate = cheque.Value.IssueDate,
                Lines = { Rows.Copy(cheque.Value, new CreateChequeLineDto { LineNo = 1 }) }
            }));
        }

        public Result<ChequeDocumentResultDto> Create(CreateChequeDocumentDto dto) => _cheques.CreateBatch(dto, _direction);

        public Result Update(CreateChequeDocumentDto dto)
        {
            var input = Check.Valid(dto,
                new Field<CreateChequeDocumentDto>(x => x.Lines, "", Must: d => d.Lines?.Count > 0, Message: "Str.Cheque.DocumentNoLine"));
            return input.Then(() => _cheques.UpdateUnmoved(dto.Id, dto.Lines[0], dto.DocDate));
        }

        public Result Delete(int id) => _cheques.DeleteUnmoved(id);
    }

    public class ChequeReceiptDocumentService : ChequeDocumentServiceBase, IChequeReceiptDocumentService
    {
        public ChequeReceiptDocumentService(IChequeService cheques) : base(cheques, ChequeDirection.Incoming) { }
    }

    public class ChequeIssueDocumentService : ChequeDocumentServiceBase, IChequeIssueDocumentService
    {
        public ChequeIssueDocumentService(IChequeService cheques) : base(cheques, ChequeDirection.Outgoing) { }
    }
}
