using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Windows.Networking.Sockets;
using Windows.Storage;
using Windows.Storage.Streams;
using Windows.ApplicationModel;
using System.Reflection;
using Newtonsoft.Json;

namespace Server
{
    public class Webserver : IDisposable
    {
        FileManagement fileManagement = new FileManagement();
        const uint BufferSize = 8192;


        StreamSocketListener listener;

        public delegate Task RouteCallback(Request req, Response res);
        List<Tuple<string, RouteCallback>> getRoutes = new List<Tuple<string, RouteCallback>>();
        List<Tuple<string, RouteCallback>> postRoutes = new List<Tuple<string, RouteCallback>>();

        private string jquery = string.Empty;
        private string common = string.Empty;
        private string rest = string.Empty;
        private string controls = string.Empty;
        private string iot_rest = string.Empty;
        private string myCss = string.Empty;
        private bool needResponse;


        public async Task ResponseImage(Request req, Response res, StorageFolder publicJsFolder)
        {
            try
            {
                var returnPageCode = string.Empty;
                string requestedFile = req.Path.LastIndexOf('?') > 0 ? req.Path.Substring(0, req.Path.LastIndexOf('?')) : req.Path;
                string filePath = requestedFile.Replace('/', '\\');
                var ret = await fileManagement.GetBinaryFile(requestedFile);
                var t = filePath.Substring(filePath.LastIndexOf("."));
                var mimeTipe = string.Empty;
                switch (t)
                {
                    case ".png":
                        mimeTipe = @"image/png";
                        break;
                    default:
                        break;
                }
                await res.SendBinaryFileContentAsync(ret, mimeTipe);
            }
            catch (FileNotFoundException)
            {
                await res.SendStatusAsync(404);
            }
            catch (Exception ex)
            {
                await res.SendStatusAsync(500);
            }
        }



        public Webserver()
        {
            listener = new StreamSocketListener();
            listener.Control.KeepAlive = true;
            listener.Control.NoDelay = true;
            listener.Control.OutboundBufferSizeInBytes = 65536;
            listener.Control.OutboundUnicastHopLimit = 255;
            listener.ConnectionReceived += (s, e) => { ProcessRequestAsync(e.Socket); };
        }


        public void Init()
        {

        }

        public async void Listen(int port)
        {
            try
            {
                await listener.BindServiceNameAsync(port.ToString());
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
            }
        }

        public void Dispose()
        {
            listener.Dispose();
        }

        public void Get(string path, RouteCallback callback)
        {
            getRoutes.Add(Tuple.Create(path, callback));
        }

        public void Post(string path, RouteCallback callback)
        {
            postRoutes.Add(Tuple.Create<string, RouteCallback>(path, callback));
        }

        static int counter = 0;
        private async void ProcessRequestAsync(StreamSocket socket)
        {
            try
            {
                StringBuilder request = new StringBuilder();
                byte[] data = new byte[BufferSize];
                IBuffer buffer = data.AsBuffer();
                uint dataRead = BufferSize;

                using (IInputStream input = socket.InputStream)
                {
                    while (dataRead == BufferSize)
                    {
                        await Task.Delay(50);
                        var result = await input.ReadAsync(buffer, BufferSize, InputStreamOptions.Partial);
                        request.Append(Encoding.UTF8.GetString(data, 0, (int)result.Length));
                        dataRead = result.Length;
                    }
                }

                // Costruiamo le nostre request e response 
                var req = new Request(request.ToString());
                var res = new Response(socket);

                if (req.Method == null)
                {
                    await res.SendStatusAsync(400); // Bad request
                    return;
                }


                var path = req.Path;
                bool handled = false;

                // Gestione delle routes
                switch (req.Method)
                {
                    case "GET":
                        // Togliamo la querystring che eventualmente possiamo gestire a parte
                        path = path.LastIndexOf('?') > 0 ? path.Substring(0, path.LastIndexOf('?')) : path;
                        var parts = path.Split('/');
                        foreach (var t in getRoutes)
                        {
                            var fileName = parts[parts.Length - 1];
                            if (t.Item1.LastIndexOf('.') > 0 && fileName.LastIndexOf('.') > 0)
                            {
                                var extension1 = fileName.Substring(fileName.LastIndexOf('.'));
                                var extensionExpected = t.Item1.Substring(t.Item1.LastIndexOf('.'));
                                if (extension1 == extensionExpected)
                                {
                                    await t.Item2(req, res);
                                    handled = true;
                                    break;
                                }
                            }
                            if (t.Item1 == path)
                            {
                                await t.Item2(req, res);
                                handled = true;
                                break;
                            }
                        }
                        if (!handled)
                        {
                            await res.SendStatusAsync(404);
                        }
                        break;

                    case "POST":
                        foreach (var t in postRoutes)
                        {
                            if (t.Item1 == path)
                            {
                                await t.Item2(req, res);
                                handled = true;
                                break;
                            }
                        }
                        if (!handled)
                        {
                            await res.SendStatusAsync(404);
                        }
                        break;
                    default:
                        Debug.WriteLine("HTTP method not supported: " + req.Method);
                        await res.SendStatusAsync(501);//Not implemented
                        break;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
            }
            finally
            {
                await socket.CancelIOAsync();
                socket.Dispose();
            }
        }
        
        public async Task ResponseHome(Request req, Response res, StorageFolder root)
        {
            try
            {
                var returnPageCode = string.Empty;

                string requestedFile = req.Path;
                if (requestedFile == "/")
                {
                    requestedFile += "home.html";
                }
                string filePath = requestedFile.Replace('/', '\\');
                using (Stream fs = await root.OpenStreamForReadAsync(filePath))
                {
                    try
                    {
                        if (requestedFile.StartsWith("/"))
                        {
                            requestedFile = requestedFile.Substring(1);
                        }
                        returnPageCode = await fileManagement.GetFile("html/home.html");
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine(ex.Message);
                    }
                    await res.SendFileContentAsync(returnPageCode);
                }
            }
            catch (FileNotFoundException)
            {
                await res.SendStatusAsync(404);
            }
            catch (Exception)
            {
                await res.SendStatusAsync(500);
            }
        }

        public async Task ResponseHtml(Request req, Response res, StorageFolder root)
        {
            try
            {
                var returnPageCode = string.Empty;

                string requestedFile = req.Path;
                if (requestedFile == "/")
                {
                    requestedFile += "home.html";
                }
                string filePath = requestedFile.Replace('/', '\\');
                using (Stream fs = await root.OpenStreamForReadAsync(filePath))
                {
                    try
                    {
                        if (requestedFile.StartsWith("/"))
                        {
                            requestedFile = requestedFile.Substring(1);
                        }
                        returnPageCode = await fileManagement.GetFile("html/" + requestedFile);
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine(ex.Message);
                    }
                    await res.SendFileContentAsync(returnPageCode);
                }
            }
            catch (FileNotFoundException)
            {
                await res.SendStatusAsync(404);
            }
            catch (Exception)
            {
                await res.SendStatusAsync(500);
            }
        }

        public async Task RefreshStatus(Request req, Response res)
        {
            try
            {
                await res.SendFileContentAsync("OK");
            }
            catch (FileNotFoundException)
            {
                await res.SendStatusAsync(404);
            }
            catch (Exception)
            {
                await res.SendStatusAsync(500);
            }
        }

        
    }
}
