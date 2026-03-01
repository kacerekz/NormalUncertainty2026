using NormalUncertainty.Scenario;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NormalUncertainty.Estimators
{
    public interface IUncertaintyEstimator3D
    {
        /// <summary>
        /// Estimates the normal uncertainty (U_f) in radians for a given scenario.
        /// </summary>
        float Estimate(Scenario3D scenario);
    }
}
