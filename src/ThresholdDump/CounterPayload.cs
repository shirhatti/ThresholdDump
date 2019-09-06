// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;

namespace ThresholdDump
{
    internal interface ICounterPayload
    {
        string GetName();
        string GetValue();
        string GetDisplay();
    }


    internal class CounterPayload : ICounterPayload
    {
        public string _name;
        public string _value;
        public string _displayName;
        public CounterPayload(IDictionary<string, object> payloadFields)
        {
            _name = payloadFields["Name"].ToString();
            _value = payloadFields["Mean"].ToString();
            _displayName = payloadFields["DisplayName"].ToString();
        }

        public string GetName()
        {
            return _name;
        }

        public string GetValue()
        {
            return _value;
        }

        public string GetDisplay()
        {
            return _displayName;
        }
    }

    internal class IncrementingCounterPayload : ICounterPayload
    {
        public string _name;
        public string _value;
        public string _displayName;
        public string _displayRateTimeScale;
        public IncrementingCounterPayload(IDictionary<string, object> payloadFields)
        {
            _name = payloadFields["Name"].ToString();
            _value = payloadFields["Increment"].ToString();
            _displayName = payloadFields["DisplayName"].ToString();
            _displayRateTimeScale = TimeSpan.Parse(payloadFields["DisplayRateTimeScale"].ToString()).ToString("%s' sec'");
        }

        public string GetName()
        {
            return _name;
        }

        public string GetValue()
        {
            return _value;
        }

        public string GetDisplay()
        {
            return $"{_displayName} / {_displayRateTimeScale}";
        }
    }
}