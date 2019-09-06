using Microsoft.Diagnostics.Tools.Dump;
using Microsoft.Diagnostics.Tools.RuntimeClient;
using Microsoft.Diagnostics.Tracing;
using Microsoft.Diagnostics.Tracing.Parsers;
using System;
using System.Collections.Generic;
using System.Diagnostics.Tracing;
using System.Linq;
using System.Threading.Tasks;

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

            if (!EventPipeClient.ListAvailablePorts().Contains(processId.Value))
            {
                throw new ArgumentException($"{nameof(processId)} is not a valid .NET Core process");
            }

            var providerList = new List<Provider>()
                {
                    new Provider(name: "System.Runtime",
                                keywords: ulong.MaxValue,
                                eventLevel: EventLevel.Informational,
                                filterData: "EventCounterIntervalSec=1"),
                    new Provider(name: "Microsoft-Windows-DotNETRuntime",
                                 keywords: (ulong)ClrTraceEventParser.Keywords.GC,
                                 eventLevel: EventLevel.Verbose)
                };

            var configuration = new SessionConfigurationV2(
                circularBufferSizeMB: 100,
                format: EventPipeSerializationFormat.NetTrace,
                requestRundown: false,
                providers: providerList);

            var stream = EventPipeClient.CollectTracing2(processId.Value, configuration, out var sessionId);
            var source = new EventPipeEventSource(stream);
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
                source.Dispose();
                EventPipeClient.StopTracing(processId.Value, sessionId);
            }

            if (tcs.Task.IsCompletedSuccessfully)
            {
                await Dumper.Collect(processId.Value, true, Dumper.DumpTypeOption.Mini);
            }
        }
    }
}
