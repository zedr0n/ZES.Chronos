using System;
using System.Collections.Generic;
using System.Linq;
using NodaTime;

namespace Chronos.Accounts;

public static class IrrSolver
{
   private static double NewtonRaphson(Func<double, (double f, double df)> func, double x0, double lower = -0.99, double upper = 10.0, double tol = 1e-8, int maxIter = 100)
   {
      var x = x0;
      for (var i = 0; i < maxIter; i++)
      {
         var (f, df) = func(x);
         if (Math.Abs(df) < 1e-12) break;
         var xNew = Math.Clamp(x - f / df, lower, upper);
         if (Math.Abs(xNew - x) < tol) return xNew;
         x = xNew;
      }
      return x;
   }        
        
   public static double Solve(List<(Instant time, double amount)> cashflows)
   {
      if (cashflows.Count < 2)
         return 0.0;

      var t0 = cashflows.Min(c => c.time);
      var normalised = cashflows
         .Select(c => ((c.time - t0).TotalSeconds / (365.25 * 24 * 3600), c.amount))
         .ToList();

      var years = (cashflows.Max(c => c.time) - cashflows.Min(c => c.time)).TotalSeconds / (365.25 * 24 * 3600);
      if (years == 0)
         return 0.0;
      
      var invested = -cashflows.Where(c => c.amount < 0).Sum(c => c.amount);
      var gain = cashflows.Where(c => c.amount > 0).Sum(c => c.amount);
      var r0 = Math.Pow(gain / invested, 1.0 / years) - 1;

      return NewtonRaphson(F, r0);

      (double, double) F(double x)
      {
         var npv = 0.0;
         var dnpv = 0.0;
         foreach (var (t, cf) in normalised) {
            var discount = Math.Pow(1 + x, t);
            npv += cf / discount;
            dnpv -= t * cf / (discount * (1 + x));
         }

         return (npv, dnpv);
      }
   }
}