// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.Diagnostics.Tracing;
using System.Collections.Generic;

namespace ThresholdDump
{
    internal static class TraceEventExtensions
    {
        internal static ICounterPayload GetCounter(this TraceEvent traceEvent)
        {
            var payloadVal = (IDictionary<string, object>)(traceEvent.PayloadValue(0));
            var payloadFields = (IDictionary<string, object>)(payloadVal["Payload"]);
            ICounterPayload payload;
            if (payloadFields.ContainsKey("CounterType"))
            {
                payload = payloadFields["CounterType"].Equals("Sum") ? (ICounterPayload)new IncrementingCounterPayload(payloadFields) : (ICounterPayload)new CounterPayload(payloadFields);
            }
            else
            {
                payload = payloadFields.Count == 6 ? (ICounterPayload)new IncrementingCounterPayload(payloadFields) : (ICounterPayload)new CounterPayload(payloadFields);
            }
            return payload;
        }
    }
}