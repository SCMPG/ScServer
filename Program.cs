using Engine;
using Engine.Serialization;

var quit = false;
new SCMPG.Server(false);
Log.Information("Press Escape is Quit");
Log.Information(Archive.IsTypeSerializable(typeof(SCMPG.GameListMessage)));
while (!quit) {
    if (Console.KeyAvailable)
    {
        ConsoleKeyInfo key = Console.ReadKey(true);
        switch (key.Key)
        {
            case ConsoleKey.Escape:
                Console.WriteLine("You pressed Escape!");
                quit = true;
                break;
            default:
                break;
        }
    }


}