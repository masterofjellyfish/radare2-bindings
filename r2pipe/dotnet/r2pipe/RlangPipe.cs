﻿using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Pipes;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
#if __MonoCS__
using Mono.Unix;
#endif

namespace r2pipe
{
    class RlangPipe : IR2Pipe
    {
#if __MonoCS__
        UnixStream ureadStream;
        UnixStream uwriteStream;
#else
        NamedPipeClientStream inclient;
#endif
        StreamReader reader;
        StreamWriter writer;

        /// <summary>
        /// Initializes a new instance of the <see cref="RlangPipe"/> class.
        /// </summary>
        public RlangPipe()
        {
#if __MonoCS__
            ureadStream = new UnixStream(int.Parse(Environment.GetEnvironmentVariable("R2PIPE_IN")));
            reader = new StreamReader(ureadStream);
           
            uwriteStream = new UnixStream(int.Parse(Environment.GetEnvironmentVariable("R2PIPE_OUT")));
            writer = new StreamWriter(uwriteStream);
#else
            // Using named pipes on windows. I like this.
            inclient = new NamedPipeClientStream("R2PIPE_IN");
            reader = new StreamReader(inclient);
            writer = new StreamWriter(inclient);
#endif
        }

        /// <summary>
        /// Executes given RunCommand in radare2
        /// </summary>
        /// <param name="command">The command to execute.</param>
        /// <returns>
        /// Returns a string
        /// </returns>
        public string RunCommand(string command)
        {
            var sb = new StringBuilder();
            writer.WriteLine(command);
            writer.Flush();
            
            while (true)
            {
                char buffer = (char)reader.Read();

                if (buffer == 0x00)
                    break;

                sb.Append(buffer);
            }
            return sb.ToString();
        }

        /// <summary>
        /// Executes given RunCommand in radare2 asynchronously
        /// </summary>
        /// <param name="command">The command to execute.</param>
        /// <returns>
        /// Returns a string
        /// </returns>
        public async Task<string> RunCommandAsync(string command)
        {
            StringBuilder builder = new StringBuilder();
            await writer.WriteLineAsync(command);
            await writer.FlushAsync();
            while (true)
            {
                char[] buffer = new char[1024];

                int length = await reader.ReadAsync(buffer, 0, 1024);

                for (int i = 0; i < length; i++)
                {
                    if (buffer[i] == 0x00)
                        goto outer;

                    builder.Append(buffer[i]);
                }
            }
        outer:
            return builder.ToString();
        }

        public void Dispose()
        {
            reader.Dispose();
            writer.Dispose();
#if __MonoCS__
            ureadStream.Dispose();
            uwriteStream.Dispose();
#else
            inclient.Dispose();
#endif
        }
    }
}