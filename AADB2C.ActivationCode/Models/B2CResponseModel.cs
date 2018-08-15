﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Threading.Tasks;

namespace AADB2C.ActivationCode.Models
{
    public class B2CResponseModel
    {
        public string version { get; set; }
        public int status { get; set; }
        public string userMessage { get; set; }
        public string verificationCode {get; set;}

        public B2CResponseModel(string message, HttpStatusCode status): this(message, null, status) 
        {
        }

        public B2CResponseModel(string message,string verificationCode, HttpStatusCode status)
        {
            this.userMessage = message;
            this.status = (int)status;
            this.verificationCode = verificationCode;
            this.version = Assembly.GetExecutingAssembly().GetName().Version.ToString();
        }
    }
}
