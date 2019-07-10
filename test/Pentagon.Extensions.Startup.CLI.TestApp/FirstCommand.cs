﻿namespace Pentagon.Extensions.Startup.CLI.TestApp {
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using Cli;
    using Microsoft.Extensions.Options;

    public class FirstCommand : CliHandler<FirstOptions>
    {
        readonly IOptionsMonitor<LolOptions> _optionsMonitor;

        public FirstCommand(IOptionsMonitor<LolOptions> optionsMonitor)
        {
            _optionsMonitor = optionsMonitor;

            _optionsMonitor.OnChange((lolOptions, name) =>
                                     {
                                         Console.WriteLine($"Changed: '{name}' value: {lolOptions.Lol ?? "null"}");
                                     });
        }

        /// <inheritdoc />
        public override async Task<int> RunAsync(FirstOptions options, CancellationToken cancellationToken = default)
        {
            Program.App.UpdateOptions<LolOptions>(o => { o.Lol = "first"; });
            Program.App.UpdateOptions<LolOptions>(o => { o.Lol = "second"; });
            Program.App.UpdateOptions<LolOptions>(o => { o.Lol = null; });

            await Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken);

            return 1;
        }
    }
}