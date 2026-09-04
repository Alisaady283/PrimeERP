using System.Linq;
using PrimeERP.Application.DTOs.Cheques;
using PrimeERP.Domain.Enums;
using PrimeERP.Domain.Results;

namespace PrimeERP.Application.Services.Cheques
{
    /// <summary>مستند "استلام/صرف شيكات" — غلاف رقيق فوق IChequeService بالشكل الذي يستهلكه محرّر المستندات
    /// (Create/GetById/GetPaged). لا جدول مستند مستقل عمداً: الشيك نفسه هو السجل، والمستند وسيلة إدخال
    /// عدة شيكات دفعة واحدة. التعديل يقع على الشيك بحركاته لا على الدفعة.</summary>
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
            if (cheque.IsFailure) return Result.Fail<CreateChequeDocumentDto>(cheque.ErrorMessage, cheque.ErrorCode);

            return Result.Ok(new CreateChequeDocumentDto
            {
                Id = id, DocDate = cheque.Value.IssueDate, Notes = cheque.Value.Notes,
                Lines = { new CreateChequeLineDto
                {
                    LineNo = 1, ChequeNo = cheque.Value.ChequeNo, BankName = cheque.Value.BankName,
                    Amount = cheque.Value.Amount, DueDate = cheque.Value.DueDate, Notes = cheque.Value.Notes
                } }
            });
        }

        public Result<ChequeDocumentResultDto> Create(CreateChequeDocumentDto dto) => _cheques.CreateBatch(dto, _direction);

        public Result Update(CreateChequeDocumentDto dto) =>
            Result.Fail("الشيك يُعدَّل بحركاته (إيداع/تحصيل/ارتداد) لا بتعديل مستند الاستلام", ErrorCode.ValidationFailed);

        public Result Delete(int id) =>
            Result.Fail("الشيك يُلغى بحركة (ارتداد/رد) لا بالحذف", ErrorCode.ValidationFailed);
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
