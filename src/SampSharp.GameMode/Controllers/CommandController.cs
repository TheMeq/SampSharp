﻿// SampSharp
// Copyright 2015 Tim Potze
// 
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
// 
//     http://www.apache.org/licenses/LICENSE-2.0
// 
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using System;
using System.Linq;
using System.Reflection;
using SampSharp.GameMode.Events;
using SampSharp.GameMode.SAMP.Commands;
using SampSharp.GameMode.Tools;
using SampSharp.GameMode.World;

namespace SampSharp.GameMode.Controllers
{
    /// <summary>
    ///     A controller processing all commands.
    /// </summary>
    public class CommandController : Disposable, IEventListener
    {
        /// <summary>
        ///     Registers the events this GlobalObjectController wants to listen to.
        /// </summary>
        /// <param name="gameMode">The running GameMode.</param>
        public virtual void RegisterEvents(BaseMode gameMode)
        {
            Console.WriteLine("Loaded {0} commands.",
                RegisterCommands(Assembly.GetAssembly(gameMode.GetType())));

            gameMode.PlayerCommandText += gameMode_PlayerCommandText;
        }

        /// <summary>
        ///     Loads all commands from the given assembly.
        /// </summary>
        /// <param name="typeInAssembly">A type inside the assembly of who to load the commands from.</param>
        /// <returns>The number of commands loaded.</returns>
        public int RegisterCommands(Type typeInAssembly)
        {
            return RegisterCommands(Assembly.GetAssembly(typeInAssembly));
        }

        /// <summary>
        ///     Loads all commands from the given assembly.
        /// </summary>
        /// <param name="assembly">The assembly of who to load the commands from.</param>
        /// <returns>The number of commands loaded.</returns>
        public int RegisterCommands(Assembly assembly)
        {
            if (assembly == null) throw new ArgumentNullException("assembly");

            try
            {
                int commandsCount = 0;
                //Detect commands in assembly containing the gamemode
                foreach (MethodInfo method in assembly.GetTypes().SelectMany(t => t.GetMethods())
                    .Where(m => m.IsStatic && m.GetCustomAttributes(typeof (CommandAttribute), false).Length > 0))
                {
                    new DetectedCommand(method);
                    commandsCount++;
                }

                return commandsCount;
            }
            catch (Exception)
            {
                /*
                 * If there are no non-static types in the given assembly,
                 * in some cases this statement throws an exception.
                 * We dismiss it and assume no commands were registered.
                 */

                return 0;
            }
        }

        /// <summary>
        ///     Loads all commands from the assembly of the given type.
        /// </summary>
        /// <typeparam name="T">A type inside the assembly of who to load the commands from.</typeparam>
        /// <returns>The number of commands loaded.</returns>
        public int RegisterCommands<T>() where T : class
        {
            return RegisterCommands(typeof (T));
        }

        private void gameMode_PlayerCommandText(object sender, CommandTextEventArgs e)
        {
            var player = sender as GtaPlayer;
            if (player == null) return;

            string text = e.Text.Substring(1);

            foreach (Command cmd in Command.All.Where(c => c.HasPlayerPermissionForCommand(player)))
            {
                string args = text;
                if (cmd.CommandTextMatchesCommand(ref args))
                {
                    if (cmd.RunCommand(player, args))
                    {
                        e.Success = true;
                        break;
                    }
                }
            }
        }

        /// <summary>
        ///     Performs tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        /// <param name="disposing">Whether managed resources should be disposed.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                foreach (Command cmd in Command.All)
                {
                    cmd.Dispose();
                }
            }
        }
    }
}