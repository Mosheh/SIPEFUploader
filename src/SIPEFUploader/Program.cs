using Ionic.Zip;
using Newtonsoft.Json;
using RestSharp;
using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;

namespace SIPEFUploader
{
    class Program
    {
        private static CancellationToken cancellationToken;

        static void Main(string[] args)
        {

            try
            {

                if (args != null && args.Length > 0)
                    UploadFile(Translate(args));
                else
                {
                    //var dados = InstruirOperador();
                    var dados = new DadosEnvio()
                    {
                        Url = "https://sipef-minas-gerais.azurewebsites.net/api/prestacaocontas/upload",
                        cnpj = "1234",
                        FilePath = @"c:\intel\prestacao.txt",
                        senhaDescompactacao = "1234"
                    };
                    if (!dados.EhValido())
                    {
                        Console.Clear();
                        Extentions.WriteError("Arquivo inexistente no caminho especificado ou Url do servidor não foi fornecida");
                        Main(null);
                    }
                    else
                    {
                        UploadFile(dados);
                    }
                }
            }
            catch (Exception ex)
            {
                Extentions.WriteError(ex);
            }

        }

        private static DadosEnvio InstruirOperador()
        {
            var dados = new DadosEnvio();
            Console.WriteLine("Nenhum comando foi fornecido para envio direto!");
            Console.WriteLine("Digite o caminho http do endpoint de prestação. Ex: http://servidor:80/api/prestacaocontas/upload");
            dados.Url = Console.ReadLine();

            Console.WriteLine(@"Digite o caminho local do seu arquivo. Ex: c:\prestacaoes\dados.txt");
            dados.FilePath = Console.ReadLine();

            Console.WriteLine(@"Digite a senha de compactação do arquivo. Ex: sipef@1010");
            dados.senhaDescompactacao = Console.ReadLine();

            Console.WriteLine(@"Digite o cnpj da organização (empresa) sem máscara");
            dados.cnpj = Console.ReadLine();

            return dados;
        }

        private static DadosEnvio Translate(string[] args)
        {
            Console.WriteLine("Executando...");
            foreach (var item in args)
            {
                Console.WriteLine(item);
            }
            return null;
        }

        private async static void UploadFile(DadosEnvio dadosEnvio)
        {
            try
            {
                var prestacao = ObterPrestacao(dadosEnvio);
                var client = new RestClient(dadosEnvio.Url);
                var request = new RestRequest(Method.POST);
                request.AddJsonBody(prestacao);

                var response = client.Post(request);
                if (response.StatusCode == HttpStatusCode.OK)
                {
                    Extentions.WriteMessage("Arquivo enviado com sucesso!");
                }
                else
                {
                    Extentions.WriteError(response.Content);
                    Main(null);
                }
            }
            catch (Exception ex)
            {
                Extentions.WriteError(ex);
            }
        }

        private static object ObterPrestacao(DadosEnvio dadosEnvio)
        {
            var arquivoDeEnvio = ObterArquivoDeEnvio(dadosEnvio);
            return new Prestacao {
                cnpj = dadosEnvio.cnpj,
                file = arquivoDeEnvio
            };
        }

        private static byte[] ObterArquivoDeEnvio(DadosEnvio dados)
        {
            var temp = Path.GetTempFileName();

           using (ZipFile zip = new ZipFile())
            {               
                zip.AddFile(dados.FilePath, "DECLARAÇÃO");
                zip.Password = dados.senhaDescompactacao;
                zip.Save(temp);
            }

            return File.ReadAllBytes(temp);
        }

        private static HttpContent CreateHttpContent(object content)
        {
            HttpContent httpContent = null;

            if (content != null)
            {
                var ms = new MemoryStream();
                SerializeJsonIntoStream(content, ms);
                ms.Seek(0, SeekOrigin.Begin);
                httpContent = new StreamContent(ms);
                httpContent.Headers.ContentType = new MediaTypeHeaderValue("application/json");
            }

            return httpContent;
        }

        public static void SerializeJsonIntoStream(object value, Stream stream)
        {
            using (var sw = new StreamWriter(stream, new UTF8Encoding(false), 1024, true))
            using (var jtw = new JsonTextWriter(sw) { Formatting = Formatting.None })
            {
                var js = new JsonSerializer();
                js.Serialize(jtw, value);
                jtw.Flush();
            }
        }

    }

    public class DadosEnvio
    {
        public string Url { get; set; }
        public string FilePath { get; set; }
        public string cnpj { get; set; }
        public string senhaDescompactacao { get; set; }

        public bool EhValido()
        {
            var dadosPreenchidos = !(string.IsNullOrEmpty(Url) && string.IsNullOrEmpty(FilePath) && string.IsNullOrEmpty(cnpj) && string.IsNullOrEmpty(senhaDescompactacao));
            return dadosPreenchidos && File.Exists(FilePath);
        }
    }

    public class Prestacao
    {
        public string cnpj { get; set; }
        public byte[] file { get; set; }
    }
}
