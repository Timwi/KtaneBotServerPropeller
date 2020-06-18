using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using RT.PropellerApi;
using RT.Servers;
using RT.Util;

namespace KTANEBotServer
{
	public class BotServer : PropellerModuleBase<BotServerSettings>
	{
		public override string Name => "KTANE Bot Server";

		private readonly object _logsLock = new object();

		private static int _updateCount;
		private static HttpResponse _updateResponse;

		public override HttpResponse Handle(HttpRequest req)
		{
			if (!req.Url.Query.Any(pair => pair.Key == "token" && pair.Value == Settings.Token))
				return HttpResponse.Empty(HttpStatusCode._401_Unauthorized);

			return new UrlResolver(
				new UrlMapping(path: "/logs", specificPath: true, handler: _ =>
				{
					lock (_logsLock)
					{
						var output = RunCommand("logs.bat", out bool success);
						if (!success)
							return HttpResponse.PlainText(output, HttpStatusCode._500_InternalServerError);

						var logs = GetBotFolder("logs.7z");
						var response = HttpResponse.Create(File.ReadAllBytes(logs), "application/x-7z-compressed");
						File.Delete(logs);
						return response;
					}
				}),
				new UrlMapping(path: "/update", specificPath: true, handler: _ =>
				{
					if (Interlocked.CompareExchange(ref _updateCount, 1, 0) == 0)
					{
						_updateResponse = HttpResponse.PlainText(RunCommand("update.bat", out bool success), success ? HttpStatusCode._200_OK : HttpStatusCode._500_InternalServerError);
						Interlocked.Decrement(ref _updateCount);
					}
					else
					{
						while (_updateCount == 1)
							continue;
					}

					return _updateResponse ?? HttpResponse.Empty(HttpStatusCode._500_InternalServerError);
				})
			).Handle(req);
		}

		private string GetBotFolder(params string[] paths) => Path.Combine(Settings.BotFolder, Path.Combine(paths));
		private string RunCommand(string command, out bool success)
		{
			var outputBuilder = new StringBuilder();
			var runner = new CommandRunner()
			{
				Command = command,
				WorkingDirectory = GetBotFolder(),
			};

			runner.StdoutText += text => outputBuilder.Append(text);
			runner.StderrText += text => outputBuilder.Append(text);

			runner.StartAndWait();

			success = runner.ExitCode == 0;

			return outputBuilder.ToString();
		}
	}

	public class BotServerSettings
	{
		public string Token;
		public string BotFolder;
	}
}
