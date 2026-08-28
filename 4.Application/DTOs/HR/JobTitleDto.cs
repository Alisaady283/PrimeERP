namespace PrimeERP.Application.DTOs.HR
{
    public class JobTitleDto
    {
        public int    Id       { get; set; }
        public string Name     { get; set; }
        public string NameEn   { get; set; }
        public bool   IsActive { get; set; }
    }

    public class CreateJobTitleDto
    {
        public string Name     { get; set; }
        public string NameEn   { get; set; }
        public bool   IsActive { get; set; } = true;
    }

    public class UpdateJobTitleDto
    {
        public int    Id       { get; set; }
        public string Name     { get; set; }
        public string NameEn   { get; set; }
        public bool   IsActive { get; set; }
    }

    public class JobTitleFilter { }
}
