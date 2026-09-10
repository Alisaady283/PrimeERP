namespace PrimeERP.Composition.Renderers
{
    /// <summary>مسار مجلد من المستخدم — نافذة النظام هي القائمة، بنفس نمط ListOutput.Export.</summary>
    public static class FolderOutput
    {
        public static string Pick(string title)
        {
            var dialog = new Microsoft.Win32.OpenFolderDialog { Title = title };
            return dialog.ShowDialog() == true ? dialog.FolderName : null;
        }
    }
}
