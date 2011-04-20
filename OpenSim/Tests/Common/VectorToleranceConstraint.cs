/*
 * Copyright (c) Contributors, http://opensimulator.org/
 * See CONTRIBUTORS.TXT for a full list of copyright holders.
 *
 * Redistribution and use in source and binary forms, with or without
 * modification, are permitted provided that the following conditions are met:
 *     * Redistributions of source code must retain the above copyright
 *       notice, this list of conditions and the following disclaimer.
 *     * Redistributions in binary form must reproduce the above copyright
 *       notice, this list of conditions and the following disclaimer in the
 *       documentation and/or other materials provided with the distribution.
 *     * Neither the name of the OpenSimulator Project nor the
 *       names of its contributors may be used to endorse or promote products
 *       derived from this software without specific prior written permission.
 *
 * THIS SOFTWARE IS PROVIDED BY THE DEVELOPERS ``AS IS'' AND ANY
 * EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED
 * WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE
 * DISCLAIMED. IN NO EVENT SHALL THE CONTRIBUTORS BE LIABLE FOR ANY
 * DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES
 * (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES;
 * LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND
 * ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT
 * (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS
 * SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
 */

using System;
using OpenMetaverse;
using NUnit.Framework;
using NUnit.Framework.Constraints;

namespace OpenSim.Tests.Common
{
    public class VectorToleranceConstraint : ANumericalToleranceConstraint
    {
        private Vector3 _baseValue;
        private Vector3 _valueToBeTested;

        public VectorToleranceConstraint(Vector3 baseValue, double tolerance) : base(tolerance)
        {
            _baseValue = baseValue;
        }

        ///<summary>
        ///Test whether the constraint is satisfied by a given value
        ///</summary>
        ///<param name="valueToBeTested">The value to be tested</param>
        ///<returns>
        ///True for success, false for failure
        ///</returns>
        public override bool Matches(object valueToBeTested)
        {
            if (valueToBeTested == null)
            {
                throw new ArgumentException("Constraint cannot be used upon null values.");
            }
            if (valueToBeTested.GetType() != typeof (Vector3))
            {
                throw new ArgumentException("Constraint cannot be used upon non vector values.");
            }

            _valueToBeTested = (Vector3) valueToBeTested;

            return (IsWithinDoubleConstraint(_valueToBeTested.X, _baseValue.X) &&
                    IsWithinDoubleConstraint(_valueToBeTested.Y, _baseValue.Y) &&
                    IsWithinDoubleConstraint(_valueToBeTested.Z, _baseValue.Z));
        }

        public override void WriteDescriptionTo(MessageWriter writer)
        {
            writer.WriteExpectedValue(
                string.Format("A value {0} within tolerance of plus or minus {1}", _baseValue, _tolerance));
        }

        public override void WriteActualValueTo(MessageWriter writer)
        {
            writer.WriteActualValue(_valueToBeTested);
        }
    }
}
