using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Networking.Sockets;

namespace Server
{
    public sealed class Response
    {
        public StreamSocket socket;

        public Response(StreamSocket socket)
        {
            this.socket = socket;
        }

        public async Task SendFileAsync(Stream file)
        {
            using (var outputStream = socket.OutputStream)
            {
                using (Stream resp = outputStream.AsStreamForWrite())
                {
                    await WriteHeaderNoFlushAsync(resp, 200, "OK", "", file.Length);
                    await file.CopyToAsync(resp);
                    await resp.FlushAsync();
                }
            }
        }

        public async Task SendFileContentAsync(string fileContent)
        {
            using (var outputStream = socket.OutputStream)
            {
                using (Stream resp = outputStream.AsStreamForWrite())
                {
                    byte[] fileContentArray = Encoding.UTF8.GetBytes(fileContent);
                    await WriteHeaderNoFlushAsync(resp, 200, "OK", "", fileContentArray.Length);
                    await resp.WriteAsync(fileContentArray, 0, fileContentArray.Length);
                    await resp.FlushAsync();
                }
            }
        }

        public async Task SendImageFileContentAsync(string fileContent)
        {
            using (var outputStream = socket.OutputStream)
            {
                using (Stream resp = outputStream.AsStreamForWrite())
                {
                    byte[] fileContentArray = Encoding.UTF8.GetBytes(fileContent);
                    await WriteHeaderNoFlushAsync(resp, 200, "OK", "", fileContentArray.Length, fileContent.Substring(fileContent.LastIndexOf('.')));
                    await resp.WriteAsync(fileContentArray, 0, fileContentArray.Length);
                    await resp.FlushAsync();
                }
            }
        }

        public async Task SendBinaryFileContentAsync(byte[] fileContent, string mimeType, string maxage = "1")
        {
            using (var outputStream = socket.OutputStream)
            {
                using (Stream resp = outputStream.AsStreamForWrite())
                {
                    await WriteHeaderNoFlushAsync(resp, 200, "OK", mimeType, fileContent.Length);
                    await resp.WriteAsync(fileContent, 0, fileContent.Length);
                    await resp.FlushAsync();
                }
            }
        }




        public async Task SendStatusAsync(int statusCode)
        {
            using (var outputStream = socket.OutputStream)
            {
                using (Stream resp = outputStream.AsStreamForWrite())
                {
                    var message = statusCode.ToString();
                    // TODO: add more status code mappings
                    switch (statusCode)
                    {
                        case 200: //Risposta standard per le richieste HTTP andate a buon fine
                            message = "OK";
                            break;
                        case 302: //Redirect
                            message = "Found";
                            break;
                        case 404: //La risorsa richiesta non è stata trovata ma in futuro potrebbe essere disponibile.
                            message = "Not Found";
                            break;
                        case 500: //Messaggio di errore generico senza dettagli
                            message = "Internal Server Error";
                            break;
                        case 501: //Il server non è in grado di soddisfare il metodo della richiesta.
                            message = "Not Implemented";
                            break;
                        case 502:
                            message = "Bad Gateway";
                            break;
                        case 503://Il server non è al momento disponibile. Generalmente è una condizione temporanea.
                            message = "Service Unavailable";
                            break;
                        case 504:
                            message = "Gateway Timeout";
                            break;
                        case 505: //Il server non supporta la versione HTTP della richiesta.
                            message = "HTTP Version Not Supported";
                            break;
                    }

                    await WriteHeaderNoFlushAsync(resp, statusCode, message, @"text/html");
                    await resp.FlushAsync();
                }
            }
        }

        //https://varvy.com/pagespeed/cache-control.html
        private Task WriteHeaderNoFlushAsync(Stream resp, int statusCode, string message, string contentType, long contentLength = 0, string location = "")
        {
            var cct = "";
            if (contentType.Length == 0)
            {
                cct = @"text/html";
            }
            else
            {
                cct = $"image/{contentType}";
            }

            string header = String.Format(
                "HTTP/1.1 {0} {1}\r\n" +
                (!String.IsNullOrEmpty(location) ? "Location: " + location + "\r\n" : string.Empty) +
                "Cache-Control:public, max-age=31536000\r\n" + 
                "Content-Length: {2}\r\n" +
                (!String.IsNullOrEmpty(contentType) ? $"Content-Type: {cct}\r\n" : string.Empty) +
                "Connection: close\r\n\r\n",
                statusCode, message,
                contentLength);
            byte[] headerArray = Encoding.UTF8.GetBytes(header);
            return resp.WriteAsync(headerArray, 0, headerArray.Length);
        }




    }
}
