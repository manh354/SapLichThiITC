using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using MudBlazor;

namespace WebAppBootstrap.Components.FileUpload
{
    public partial class FileUploadPurduePage
    {
        [Inject] ISnackbar? Snackbar { get; set; }
        private IBrowserFile? DatasetFile { get; set; }

        void UploadFile(IBrowserFile file)
        {
            DatasetFile = file;
        }

        void ProcessUploadedFile()
        {
            if (DatasetFile is null)
            {
                Snackbar?.Add("Chưa có file nào được tải lên", Severity.Error);
                return;
            }

        }
    }
}