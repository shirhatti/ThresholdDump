using Microsoft.Diagnostics.NETCore.Client;
using Microsoft.Diagnostics.Tools.Dump;
using Microsoft.Diagnostics.Tracing;
using Microsoft.Diagnostics.Tracing.Parsers;
using System;
using System.Collections.Generic;
using System.Diagnostics.Tracing;
using System.Linq;
using System.Threading.Tasks;
using static Microsoft.Diagnostics.Tools.Dump.Dumper;

namespace ThresholdDump
{
    class Program
    {
        /// <summary>
        /// Application to take a process dump if CPU utilization exceeds specified percentage
        /// </summary>
        /// <param name="processId">Process Id of the the tracee process</param>
        /// <param name="cpu">The CPU utilization percentage on which to trigger a dump. The default value is 10%</param>
        static async Task Main(int? processId, int cpu = 10)
        {
            if (processId! == null)
            {
                throw new ArgumentNullException(nameof(processId));
            }
            if (!DiagnosticsClient.GetPublishedProcesses().Contains(processId.Value))
            {
                throw new ArgumentException($"{nameof(processId)} is not a valid .NET process");
            }

            var providerList = new List<EventPipeProvider>()
                {
                    new EventPipeProvider(name: "System.Runtime",
                                keywords: long.MaxValue,
                                eventLevel: EventLevel.Informational,
                                arguments: new Dictionary<string, string>() { { "EventCounterIntervalSec", "1" } }),
                    new EventPipeProvider(name: "Microsoft-Windows-DotNETRuntime",
                                 keywords: (long)ClrTraceEventParser.Keywords.GC,
                                 eventLevel: EventLevel.Verbose)
                };
            var diagnosticsClient = new DiagnosticsClient(processId.Value);
            var session = diagnosticsClient.StartEventPipeSession(
                            providers: providerList,
                            requestRundown: false);

            var source = new EventPipeEventSource(session.EventStream);
            var tcs = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            Console.CancelKeyPress += delegate (object sender, ConsoleCancelEventArgs e)
            {
                tcs.TrySetCanceled();
            };
            source.Dynamic.AddCallbackForProviderEvent("System.Runtime", "EventCounters", (traceEvent) =>
            {
                var counter = traceEvent.GetCounter();
                if (counter.GetName() == "cpu-usage")
                {
                    Console.WriteLine($"{counter.GetName()}\t{counter.GetValue()}");
                    if (Int32.Parse(counter.GetValue()) >= cpu)
                    {
                        source.StopProcessing();
                        tcs.SetResult(true);
                    }
                }
            });
            _ = Task.Run(() => source.Process());
            try
            {
                _ = await tcs.Task;
            }
            catch (Exception e) when (e is TaskCanceledException)
            {

                Console.WriteLine("Cancelled due to Ctrl+C");
            }
            finally
            {
                session.Dispose();
                source.Dispose();
            }

            if (tcs.Task.IsCompletedSuccessfully)
            {
                new Dumper().Collect(processId.Value, false, DumpTypeOption.Mini);
            }
        }
    }
}
