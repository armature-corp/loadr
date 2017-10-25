using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;

namespace loadr.CLI
{
    public class Program
    {
        static void Main(string[] args)
        {
            string iterationsString = null;
            string urlString = null;

            if (args != null && args.Length > 0 && args.Any(o => o.Equals("help", StringComparison.InvariantCultureIgnoreCase)))
            {
                Log("Usage: loadr <iterations> <url>");
            }
            else
            {
                if (args.Length == 2)
                {
                    iterationsString = args[0];
                    urlString = args[1];
                }
                else
                {
                    Console.Write("# of iterations? ");
                    iterationsString = Console.ReadLine();

                    Console.Write("            URL? ");
                    urlString = Console.ReadLine();
                }

                if (!int.TryParse(iterationsString, out int iterations))
                {
                    Log("Iterations must be an int.");
                }

                if (!Uri.TryCreate(urlString, UriKind.Absolute, out Uri url))
                {
                    Log("URL is invalid.");
                }

                RunLoadTestAsync(iterations, url)
                    .GetAwaiter()
                    .GetResult();
            }

#if DEBUG
            Log();
            Log("Done. Press any key to exit.");

            Console.ReadKey();
#endif
        }

        private static async Task RunLoadTestAsync(int iterations, Uri url)
        {
            var timings = new ConcurrentBag<long>();

            using (var client = new HttpClient())
            {
                var tasks = Enumerable
                    .Range(0, iterations)
                    .Select(num => Task.Run(() =>
                    {
                        var request = WebRequest.Create(url);
                        var stopwatch = Stopwatch.StartNew();

                        request.Headers.Add(HttpRequestHeader.CacheControl, "no-cache, no-store, must-revalidate");

                        using (var response = (HttpWebResponse)request.GetResponse())
                        {
                            stopwatch.Stop();

                            var elapsed = stopwatch.ElapsedMilliseconds;

                            Log($"#{num}\t{response.StatusCode}\t{elapsed:N0}");

                            timings.Add(elapsed);
                        }
                    }))
                    .ToList();

                await Task.WhenAll(tasks);
            }

            Log();
            Log($"    Min: {timings.Min():N2}");
            Log($"Average: {timings.Average():N2}");
            Log($"    Max: {timings.Max():N2}");
        }

        private static void Log(string message = null)
        {
            Console.WriteLine(message);
        }
    }
}
