using System;
using System.Collections.Generic;
using System.Text;
using MillisecondFunctions.Models;

namespace MillisecondFunctions
{
    public static class Helper
    {
        public static Customer FromDTOtoCustomer(DTO input)
        {
            StringBuilder sb = new StringBuilder();

            foreach (var item in input.Attributes)
            {
                sb.Append(item + ";");
            }

            Customer c = new Customer()
            {
                Key = input.Key,
                Email = input.Email,
                Attributes = sb.ToString()
            };

            return c;
        }
    }
}
