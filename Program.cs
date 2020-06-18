using RT.PropellerApi;
using RT.Util;

namespace KTANEBotServer
{
	class Program
	{
		static void Main(string[] args)
		{
			PropellerUtil.RunStandalone(PathUtil.AppPathCombine("Bot-Propeller-standalone.json"), new BotServer());
		}
	}
}
