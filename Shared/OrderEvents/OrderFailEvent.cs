﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Shared.OrderEvents
{
    public class OrderFailEvent
    {
        public int OrderId { get; set; }    
        public string Message { get; set; }
    }
}
