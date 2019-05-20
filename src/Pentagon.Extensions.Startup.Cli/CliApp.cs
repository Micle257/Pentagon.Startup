﻿// -----------------------------------------------------------------------
//  <copyright file="AppConsoleCore.cs">
//   Copyright (c) Michal Pokorný. All Rights Reserved.
//  </copyright>
// -----------------------------------------------------------------------

namespace Pentagon.Extensions.Startup.Cli
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;
    using System.Runtime.InteropServices;
    using System.Threading;
    using System.Threading.Tasks;
    using CommandLine;
    using Console;
    using JetBrains.Annotations;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Logging;

    public class CliApp : AppCore
    {
        /// <summary>
        /// Gets the fail callback. Default callback is no action.
        /// </summary>
        /// <value>
        /// The <see cref="Func{TResult}"/> with return value of <see cref="Task"/>.
        /// </value>
        public virtual Func<IEnumerable<Error>, Task<int>> FailCallback { get; protected set; } = errors =>
                                                                                                  {
                                                                                                      ConsoleHelper.WriteError(errorValue: $"CLI parsing failed:{errors?.Aggregate(string.Empty, (a, b) => $" {a}\n {b}") ?? "\n Unknown reason"}");
                                                                                                      return Task.FromResult(-1);
                                                                                                  };

        /// <inheritdoc />
        protected override void BuildApp(IApplicationBuilder appBuilder, string[] args)
        {
            appBuilder.AddCliCommands();
        }

        public virtual void OnExit(bool success)
        {
            Console.WriteLine();
            Console.WriteLine();
            if (Environment.IsDevelopment())
            {
                if (!success)
                {
                    ConsoleHelper.WriteError(errorValue: "Program execution failed.");
                    Console.WriteLine();
                }

                Console.WriteLine(value: " Press any key to exit the application...");
                Console.ReadKey();
            }
            else if (!Environment.IsDevelopment() && !success)
            {
                ConsoleHelper.WriteError(errorValue: "Program execution failed.");
                Console.WriteLine();
            }
        }

        public Task<int> ExecuteCliAsync(IEnumerable<string> args, CancellationToken cancellationToken = default)
        {
            if (Services == null)
            {
                throw new ArgumentNullException($"{nameof(Services)}", $"The App is not properly built: cannot execute app.");
            }

            var types = AppDomain.CurrentDomain
                                 .GetAssemblies()
                                 .SelectMany(a => a.GetTypes())
                                .Where(a => a.GetCustomAttribute<VerbAttribute>() != null)
                                .ToArray();

            if (types.Length == 0)
            {
                ConsoleHelper.WriteError(errorValue: "No CLI verbs found in assembly.");
                return Task.FromResult(-1);
            }

            var parserResult = Parser.Default.ParseArguments(args, types);

            var result = parserResult.MapResult(o => RunCommandByReflection(o, cancellationToken), FailCallback);

            return result;
        }

        Task<int> RunCommandByReflection(object options, CancellationToken cancellationToken = default)
        {
            var optionsType = options.GetType().GetTypeInfo();

            //var commandTypes = Assembly.GetEntryAssembly()
            //                           .DefinedTypes
            //                           .Where(a => a.ImplementedInterfaces.Any(i => i == typeof(ICliCommand<>).MakeGenericType(optionsType)));
            //
            //var commandType = commandTypes.FirstOrDefault();
            //
            //if (commandType == null)
            //{
            //    ConsoleHelper.WriteError(errorValue: $"Cannot find command type for options: {optionsType.Name}.");
            //    return Task.FromResult(-1);
            //}

            var commandType = typeof(ICliCommand<>).MakeGenericType(optionsType);

            var command = Services?.GetService(commandType);

            if (command == null)
            {
                ConsoleHelper.WriteError(errorValue: $"Cannot resolve command type from DI for options: {optionsType.Name}.");
                return Task.FromResult(-1);
            }

            var runMethod = command.GetType().GetMethod(nameof(ICliCommand<object>.RunAsync));

            var methodReturnValue = (Task<int>)runMethod.Invoke(command, new[] { options, cancellationToken });

            return methodReturnValue;
        }

        public async Task<int> RunCommand<TCommand, TOptions>(TOptions options, Func<Task> failCallback)
                where TCommand : ICliCommand<TOptions>
        {
            try
            {
                var command = Services.GetService<TCommand>();

                return await command.RunAsync(options).ConfigureAwait(false);
            }
            catch (Exception e)
            {
                Services.GetService<ILogger>()?.LogCritical(e, message: "Error while running play command.");
                await (failCallback?.Invoke()).ConfigureAwait(false);
                throw;
            }
        }

        public static Task RunAsync(string[] args)
        {
            var app = new CliApp();
            app.ConfigureServices(args);
            return app.ExecuteCliAsync(args);
        }
    }
}