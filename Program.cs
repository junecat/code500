using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.IO;
using System.Net;
using Serilog;
using System.Text;

namespace ServerApp
{
    public class Program
    {
        const int portNum = 3967;

        public static void Main(string[] args)
        {
            Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Debug()
            .WriteTo.Console()
            .WriteTo.File("Logs/code500_.log", rollingInterval: RollingInterval.Day)
            .CreateLogger();

            Log.Information($"Running code500 on port {portNum}...");

            var host = new WebHostBuilder().UseKestrel(GetKso()).UseContentRoot(Directory.GetCurrentDirectory()).UseStartup<Startup>().Build();
            host.Run();
        }

        static Action<KestrelServerOptions> GetKso()
        {
            Action<KestrelServerOptions> ret = options =>
            {
                options.Limits.MaxConcurrentConnections = 100;
                options.Listen(IPAddress.Any, portNum);
            };
            return ret;
        }


    }

    public class Startup
    {
        public void ConfigureServices(IServiceCollection services)
        {
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            const string sorryMsg = "Sorry something went wrong...";
            try
            {

                app.Run(async (context) =>
                {
                    WriteDetailLog(context);
                    HostString hostString = context.Request.HttpContext.Request.Host;
                    //Log.Information($"QueryString = {hostString}");

                    // начинаем гадание на кофейной гуще - какое целое число содержит имя сайта?
                    string host = hostString.Host;
                    // for example:
                    // string host = "code404.junecat.ru";
                    string[] domains = host.Split('.');
                    if (domains.Length > 1 && !( domains[0].ToLower().IndexOf("code") == 0 || domains[1].ToLower().IndexOf("code") == 0) )
                    {
                        Log.Error($"Sorry, 'code' word not present in begin of URL");
                        await context.Response.WriteAsync(sorryMsg);
                        return;
                    }

                    int httpcode = 0;

                    List<int> availableHttpCodes = new List<int> { 401, 404, 301, 302, 410, };
                    for (int i = 500; i <= 511; ++i)
                        availableHttpCodes.Add(i);

                    string domainDigits = "";
                    if (domains[0].ToLower() == "www" && domains.Length > 1 && domains[1].ToLower().IndexOf("code") == 0)
                        domainDigits = domains[1].ToLower().Replace("code", "");
                    else
                        if (domains[0].ToLower().IndexOf("code") == 0)
                        domainDigits = domains[0].ToLower().Replace("code", "");

                    if (string.IsNullOrEmpty(domainDigits))
                    {
                        Log.Error($"Sorry, 'domainDigits' in URL={host} is empty");
                        await context.Response.WriteAsync(sorryMsg);
                        return;
                    }

                    if (int.TryParse(domainDigits, out httpcode))
                    {
                        Log.Information($"requested code: {httpcode}");

                        if (availableHttpCodes.Contains(httpcode))
                        {
                            var response = context.Response;
                            response.Headers.ContentLanguage = "ru-RU";
                            response.Headers.ContentType = "text/plain; charset=utf-8";
                            response.Headers.Append("hello-from", "junecat.ru");    // добавление кастомного заголовка
                        response.StatusCode = httpcode;
                            await response.WriteAsync(String.Empty);
                            return;
                        }
                        else
                        {
                            Log.Error($"Sorry, code {httpcode} not present in available for reuest");
                            await context.Response.WriteAsync(sorryMsg);
                            return;
                        }
                    }

                    Log.Error(sorryMsg);
                    await context.Response.WriteAsync(sorryMsg);
                });

            }
            catch (Exception ex)
            {
                Log.Error(ex.Message);
            }
        }

        private void WriteDetailLog(HttpContext context)
        {
            Log.Information($"Path =  {context.Request.Path}");
            StringBuilder sb = new StringBuilder();
            foreach (var h in context.Request.Headers)
                sb.Append($"{h.Key} = {h.Value}{Environment.NewLine}");
            string uinfo = $"host={context.Request.Host.Host}, {Environment.NewLine}headers={sb.ToString()}";
            Log.Information($"\nUser info: {uinfo}");
        }
    }
}
