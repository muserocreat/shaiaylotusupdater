using System.IO;
using Updater.Common;

namespace Updater.Core
{
    /// <summary>
    /// Represents a replacement updater application.
    /// </summary>
    public sealed class NewUpdater
    {
        public const string FileName = "new_updater.exe";
        public readonly string Path = string.Empty;
        public readonly string Url = string.Empty;

        /// <summary>
        /// Initializes a new instance of the <see cref="NewUpdater"/> class.
        /// </summary>
        public NewUpdater()
        {
            Path = System.IO.Path.Combine(Directory.GetCurrentDirectory(), FileName);
            
#if DEBUG
            // En modo DEBUG, primero verificar si existe un new_updater local para pruebas
            string localPath = System.IO.Path.Combine(Directory.GetCurrentDirectory(), "new_updater_local.exe");
            if (File.Exists(localPath))
            {
                Url = localPath;
            }
            else
            {
                Url = $"{Constants.Source}/{FileName}";
            }
#else
            Url = $"{Constants.Source}/{FileName}";
#endif
        }
    }
}
