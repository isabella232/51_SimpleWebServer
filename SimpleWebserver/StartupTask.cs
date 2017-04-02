using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net.Http;
using Windows.ApplicationModel.Background;
using Windows.Storage;
using Windows.System.Threading;
using System.Diagnostics;
using Server;

// The Background Application template is documented at http://go.microsoft.com/fwlink/?LinkID=533884&clcid=0x409

namespace SimpleWebserver
{
    public sealed class StartupTask : IBackgroundTask
    {
        StorageFolder localFolder = Windows.ApplicationModel.Package.Current.InstalledLocation;

        private static BackgroundTaskDeferral myDeferralTask = null;
        public async void Run(IBackgroundTaskInstance taskInstance)
        {
            myDeferralTask = taskInstance.GetDeferral();
            await ThreadPool.RunAsync(async workItem =>
            {
                try
                {
                    Webserver server = new Webserver();

                    server.Init();
                    var publicHtmlFolder = await localFolder.GetFolderAsync("html");
                    var publicCssFolder = await localFolder.GetFolderAsync("css");
                    var publicJsFolder = await localFolder.GetFolderAsync("js");
                    var publicImageFolder = await localFolder.GetFolderAsync("images");


                    server.Get("/", async (req, res) => { await server.ResponseHome(req, res, publicHtmlFolder); });
                    server.Get("/*.html", async (req, res) => { await server.ResponseHtml(req, res, publicHtmlFolder); });
                    server.Get("/images/*.svg", async (req, res) => { await server.ResponseImage(req, res, publicImageFolder); });
                    server.Get("/images/*.jpg", async (req, res) => { await server.ResponseImage(req, res, publicImageFolder); });
                    server.Get("/images/*.tif", async (req, res) => { await server.ResponseImage(req, res, publicImageFolder); });
                    server.Get("/images/*.gif", async (req, res) => { await server.ResponseImage(req, res, publicImageFolder); });
                    server.Get("/images/*.png", async (req, res) => { await server.ResponseImage(req, res, publicImageFolder); });

                    server.Post("/refreshStatus", async (req, res) => { await server.RefreshStatus(req, res); });

                    server.Listen(9988);
                }
                catch (Exception ex)
                {
                    Debug.WriteLine(ex.Message);
                }
            });
        }
    }
}
