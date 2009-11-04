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

/* Revised Aug, Sept 2009 by Kitto Flora. ODEDynamics.cs replaces
 * ODEVehicleSettings.cs. It and ODEPrim.cs are re-organised:
 * ODEPrim.cs contains methods dealing with Prim editing, Prim
 * characteristics and Kinetic motion.
 * ODEDynamics.cs contains methods dealing with Prim Physical motion
 * (dynamics) and the associated settings. Old Linear and angular
 * motors for dynamic motion have been replace with  MoveLinear()
 * and MoveAngular(); 'Physical' is used only to switch ODE dynamic 
 * simualtion on/off; VEHICAL_TYPE_NONE/VEHICAL_TYPE_<other> is to
 * switch between 'VEHICLE' parameter use and general dynamics
 * settings use.
 */ 

using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.InteropServices;
using log4net;
using OpenMetaverse;
using Ode.NET;
using OpenSim.Framework;
using OpenSim.Region.Physics.Manager;

namespace OpenSim.Region.Physics.OdePlugin
{
    public class ODEDynamics
    {
        public Vehicle Type
        {        
            get { return m_type; }
        }

        public IntPtr Body
        {
            get { return m_body; }
        }

        private int frcount = 0;										// Used to limit dynamics debug output to 
        																// every 100th frame

        // private OdeScene m_parentScene = null;
        private IntPtr m_body = IntPtr.Zero;
//        private IntPtr m_jointGroup = IntPtr.Zero;
//        private IntPtr m_aMotor = IntPtr.Zero;
 

        // Vehicle properties
        private Vehicle m_type = Vehicle.TYPE_NONE;						// If a 'VEHICLE', and what kind
        // private Quaternion m_referenceFrame = Quaternion.Identity;	// Axis modifier
        private VehicleFlag m_flags = (VehicleFlag) 0;					// Boolean settings:
																		// HOVER_TERRAIN_ONLY
																		// HOVER_GLOBAL_HEIGHT
																		// NO_DEFLECTION_UP
																		// HOVER_WATER_ONLY
																		// HOVER_UP_ONLY
																		// LIMIT_MOTOR_UP
																		// LIMIT_ROLL_ONLY
        
        // Linear properties
        private Vector3 m_linearMotorDirection = Vector3.Zero;			// velocity requested by LSL, decayed by time
        private Vector3 m_linearMotorDirectionLASTSET = Vector3.Zero;	// velocity requested by LSL
        private Vector3 m_dir = Vector3.Zero;							// velocity applied to body
        private Vector3 m_linearFrictionTimescale = Vector3.Zero;
        private float m_linearMotorDecayTimescale = 0;
        private float m_linearMotorTimescale = 0;
        private Vector3 m_lastLinearVelocityVector = Vector3.Zero;
		// private bool m_LinearMotorSetLastFrame = false;
        // private Vector3 m_linearMotorOffset = Vector3.Zero;
        
        //Angular properties
        private Vector3 m_angularMotorDirection = Vector3.Zero;			// angular velocity requested by LSL motor 
        private int m_angularMotorApply = 0;							// application frame counter
        private Vector3 m_angularMotorVelocity = Vector3.Zero;			// current angular motor velocity 
        private float m_angularMotorTimescale = 0;						// motor angular velocity ramp up rate
        private float m_angularMotorDecayTimescale = 0;					// motor angular velocity decay rate
        private Vector3 m_angularFrictionTimescale = Vector3.Zero;		// body angular velocity  decay rate
        private Vector3 m_lastAngularVelocity = Vector3.Zero;			// what was last applied to body
 //       private Vector3 m_lastVertAttractor = Vector3.Zero;				// what VA was last applied to body

		//Deflection properties        
        // private float m_angularDeflectionEfficiency = 0;
        // private float m_angularDeflectionTimescale = 0;
        // private float m_linearDeflectionEfficiency = 0;
        // private float m_linearDeflectionTimescale = 0;
        
        //Banking properties
        // private float m_bankingEfficiency = 0;
        // private float m_bankingMix = 0;
        // private float m_bankingTimescale = 0;
        
        //Hover and Buoyancy properties
        private float m_VhoverHeight = 0f;
//        private float m_VhoverEfficiency = 0f;
        private float m_VhoverTimescale = 0f;
        private float m_VhoverTargetHeight = -1.0f;     // if <0 then no hover, else its the current target height 
        private float m_VehicleBuoyancy = 0f;			//KF: m_VehicleBuoyancy is set by VEHICLE_BUOYANCY for a vehicle.
        			// Modifies gravity. Slider between -1 (double-gravity) and 1 (full anti-gravity) 
        			// KF: So far I have found no good method to combine a script-requested .Z velocity and gravity.
        			// Therefore only m_VehicleBuoyancy=1 (0g) will use the script-requested .Z velocity. 
        												
		//Attractor properties        												
        private float m_verticalAttractionEfficiency = 1.0f;		// damped
        private float m_verticalAttractionTimescale = 500f;			// Timescale > 300  means no vert attractor.
        
        



        internal void ProcessFloatVehicleParam(Vehicle pParam, float pValue)
        {
            switch (pParam)
            {
                case Vehicle.ANGULAR_DEFLECTION_EFFICIENCY:
                    if (pValue < 0.01f) pValue = 0.01f;
                    // m_angularDeflectionEfficiency = pValue;
                    break;
                case Vehicle.ANGULAR_DEFLECTION_TIMESCALE:
                    if (pValue < 0.01f) pValue = 0.01f;
                    // m_angularDeflectionTimescale = pValue;
                    break;
                case Vehicle.ANGULAR_MOTOR_DECAY_TIMESCALE:
                    if (pValue < 0.01f) pValue = 0.01f;
                    m_angularMotorDecayTimescale = pValue;
                    break;
                case Vehicle.ANGULAR_MOTOR_TIMESCALE:
                    if (pValue < 0.01f) pValue = 0.01f;
                    m_angularMotorTimescale = pValue;
                    break;
                case Vehicle.BANKING_EFFICIENCY:
                    if (pValue < 0.01f) pValue = 0.01f;
                    // m_bankingEfficiency = pValue;
                    break;
                case Vehicle.BANKING_MIX:
                    if (pValue < 0.01f) pValue = 0.01f;
                    // m_bankingMix = pValue;
                    break;
                case Vehicle.BANKING_TIMESCALE:
                    if (pValue < 0.01f) pValue = 0.01f;
                    // m_bankingTimescale = pValue;
                    break;
                case Vehicle.BUOYANCY:
                	if (pValue < -1f) pValue = -1f;
                	if (pValue > 1f) pValue = 1f;
                    m_VehicleBuoyancy = pValue;
                    break;
//                case Vehicle.HOVER_EFFICIENCY:
//                	if (pValue < 0f) pValue = 0f;
//                	if (pValue > 1f) pValue = 1f;
//                    m_VhoverEfficiency = pValue;
//                    break;
                case Vehicle.HOVER_HEIGHT:
                    m_VhoverHeight = pValue;
                    break;
                case Vehicle.HOVER_TIMESCALE:
                    if (pValue < 0.01f) pValue = 0.01f;
                    m_VhoverTimescale = pValue;
                    break;
                case Vehicle.LINEAR_DEFLECTION_EFFICIENCY:
                    if (pValue < 0.01f) pValue = 0.01f;
                    // m_linearDeflectionEfficiency = pValue;
                    break;
                case Vehicle.LINEAR_DEFLECTION_TIMESCALE:
                    if (pValue < 0.01f) pValue = 0.01f;
                    // m_linearDeflectionTimescale = pValue;
                    break;
                case Vehicle.LINEAR_MOTOR_DECAY_TIMESCALE:
                    if (pValue < 0.01f) pValue = 0.01f;
                    m_linearMotorDecayTimescale = pValue;
                    break;
                case Vehicle.LINEAR_MOTOR_TIMESCALE:
                    if (pValue < 0.01f) pValue = 0.01f;
                    m_linearMotorTimescale = pValue;
                    break;
                case Vehicle.VERTICAL_ATTRACTION_EFFICIENCY:
                    if (pValue < 0.1f) pValue = 0.1f;	// Less goes unstable
                    if (pValue > 1.0f) pValue = 1.0f;
                    m_verticalAttractionEfficiency = pValue;
                    break;
                case Vehicle.VERTICAL_ATTRACTION_TIMESCALE:
                    if (pValue < 0.01f) pValue = 0.01f;
                    m_verticalAttractionTimescale = pValue;
                    break;
                    
                // These are vector properties but the engine lets you use a single float value to 
                // set all of the components to the same value
                case Vehicle.ANGULAR_FRICTION_TIMESCALE:
                    m_angularFrictionTimescale = new Vector3(pValue, pValue, pValue);
                    break;
                case Vehicle.ANGULAR_MOTOR_DIRECTION:
                    m_angularMotorDirection = new Vector3(pValue, pValue, pValue);
                    m_angularMotorApply = 10;
                    break;
                case Vehicle.LINEAR_FRICTION_TIMESCALE:
                    m_linearFrictionTimescale = new Vector3(pValue, pValue, pValue);
                    break;
                case Vehicle.LINEAR_MOTOR_DIRECTION:
                    m_linearMotorDirection = new Vector3(pValue, pValue, pValue);
                    m_linearMotorDirectionLASTSET = new Vector3(pValue, pValue, pValue);
                    break;
                case Vehicle.LINEAR_MOTOR_OFFSET:
                    // m_linearMotorOffset = new Vector3(pValue, pValue, pValue);
                    break;

            }
            
        }//end ProcessFloatVehicleParam

        internal void ProcessVectorVehicleParam(Vehicle pParam, Vector3 pValue)
        {
            switch (pParam)
            {
                case Vehicle.ANGULAR_FRICTION_TIMESCALE:
                    m_angularFrictionTimescale = new Vector3(pValue.X, pValue.Y, pValue.Z);
                    break;
                case Vehicle.ANGULAR_MOTOR_DIRECTION:
                    m_angularMotorDirection = new Vector3(pValue.X, pValue.Y, pValue.Z);
                    // Limit requested angular speed to 2 rps= 4 pi rads/sec
                    if(m_angularMotorDirection.X > 12.56f) m_angularMotorDirection.X = 12.56f; 
                    if(m_angularMotorDirection.X < - 12.56f) m_angularMotorDirection.X = - 12.56f; 
                    if(m_angularMotorDirection.Y > 12.56f) m_angularMotorDirection.Y = 12.56f; 
                    if(m_angularMotorDirection.Y < - 12.56f) m_angularMotorDirection.Y = - 12.56f; 
                    if(m_angularMotorDirection.Z > 12.56f) m_angularMotorDirection.Z = 12.56f; 
                    if(m_angularMotorDirection.Z < - 12.56f) m_angularMotorDirection.Z = - 12.56f; 
                    m_angularMotorApply = 10;
                    break;
                case Vehicle.LINEAR_FRICTION_TIMESCALE:
                    m_linearFrictionTimescale = new Vector3(pValue.X, pValue.Y, pValue.Z);
                    break;
                case Vehicle.LINEAR_MOTOR_DIRECTION:
                    m_linearMotorDirection = new Vector3(pValue.X, pValue.Y, pValue.Z);
                    m_linearMotorDirectionLASTSET = new Vector3(pValue.X, pValue.Y, pValue.Z);
                    break;
                case Vehicle.LINEAR_MOTOR_OFFSET:
                    // m_linearMotorOffset = new Vector3(pValue.X, pValue.Y, pValue.Z);
                    break;
            }
            
        }//end ProcessVectorVehicleParam

        internal void ProcessRotationVehicleParam(Vehicle pParam, Quaternion pValue)
        {
            switch (pParam)
            {
                case Vehicle.REFERENCE_FRAME:
                    // m_referenceFrame = pValue;
                    break;
            }
            
        }//end ProcessRotationVehicleParam

        internal void ProcessTypeChange(Vehicle pType)
        {
			// Set Defaults For Type
            m_type = pType;
            switch (pType)
            {
                case Vehicle.TYPE_SLED:
                    m_linearFrictionTimescale = new Vector3(30, 1, 1000);
                    m_angularFrictionTimescale = new Vector3(1000, 1000, 1000);
                    m_linearMotorDirection = Vector3.Zero;
                    m_linearMotorTimescale = 1000;
                    m_linearMotorDecayTimescale = 120;
                    m_angularMotorDirection = Vector3.Zero;
                    m_angularMotorTimescale = 1000;
                    m_angularMotorDecayTimescale = 120;
                    m_VhoverHeight = 0;
//                    m_VhoverEfficiency = 1;
                    m_VhoverTimescale = 10;
                    m_VehicleBuoyancy = 0;
                    // m_linearDeflectionEfficiency = 1;
                    // m_linearDeflectionTimescale = 1;
                    // m_angularDeflectionEfficiency = 1;
                    // m_angularDeflectionTimescale = 1000;
                    // m_bankingEfficiency = 0;
                    // m_bankingMix = 1;
                    // m_bankingTimescale = 10;
                    // m_referenceFrame = Quaternion.Identity;
                    m_flags &=
                        ~(VehicleFlag.HOVER_WATER_ONLY | VehicleFlag.HOVER_TERRAIN_ONLY |
                          VehicleFlag.HOVER_GLOBAL_HEIGHT | VehicleFlag.HOVER_UP_ONLY);
                    m_flags |= (VehicleFlag.NO_DEFLECTION_UP | VehicleFlag.LIMIT_ROLL_ONLY | VehicleFlag.LIMIT_MOTOR_UP);
                    break;
                case Vehicle.TYPE_CAR:
                    m_linearFrictionTimescale = new Vector3(100, 2, 1000);
                    m_angularFrictionTimescale = new Vector3(1000, 1000, 1000);
                    m_linearMotorDirection = Vector3.Zero;
                    m_linearMotorTimescale = 1;
                    m_linearMotorDecayTimescale = 60;
                    m_angularMotorDirection = Vector3.Zero;
                    m_angularMotorTimescale = 1;
                    m_angularMotorDecayTimescale = 0.8f;
                    m_VhoverHeight = 0;
//                    m_VhoverEfficiency = 0;
                    m_VhoverTimescale = 1000;
                    m_VehicleBuoyancy = 0;
                    // // m_linearDeflectionEfficiency = 1;
                    // // m_linearDeflectionTimescale = 2;
                    // // m_angularDeflectionEfficiency = 0;
                    // m_angularDeflectionTimescale = 10;
                    m_verticalAttractionEfficiency = 1f;
                    m_verticalAttractionTimescale = 10f;
                    // m_bankingEfficiency = -0.2f;
                    // m_bankingMix = 1;
                    // m_bankingTimescale = 1;
                    // m_referenceFrame = Quaternion.Identity;
                    m_flags &= ~(VehicleFlag.HOVER_WATER_ONLY | VehicleFlag.HOVER_TERRAIN_ONLY | VehicleFlag.HOVER_GLOBAL_HEIGHT);
                    m_flags |= (VehicleFlag.NO_DEFLECTION_UP | VehicleFlag.LIMIT_ROLL_ONLY | VehicleFlag.HOVER_UP_ONLY |
                                VehicleFlag.LIMIT_MOTOR_UP);
                    break;
                case Vehicle.TYPE_BOAT:
                    m_linearFrictionTimescale = new Vector3(10, 3, 2);
                    m_angularFrictionTimescale = new Vector3(10,10,10);
                    m_linearMotorDirection = Vector3.Zero;
                    m_linearMotorTimescale = 5;
                    m_linearMotorDecayTimescale = 60;
                    m_angularMotorDirection = Vector3.Zero;
                    m_angularMotorTimescale = 4;
                    m_angularMotorDecayTimescale = 4;
                    m_VhoverHeight = 0;
//                    m_VhoverEfficiency = 0.5f;
                    m_VhoverTimescale = 2;
                    m_VehicleBuoyancy = 1;
                    // m_linearDeflectionEfficiency = 0.5f;
                    // m_linearDeflectionTimescale = 3;
                    // m_angularDeflectionEfficiency = 0.5f;
                    // m_angularDeflectionTimescale = 5;
                    m_verticalAttractionEfficiency = 0.5f;
                    m_verticalAttractionTimescale = 5f;
                    // m_bankingEfficiency = -0.3f;
                    // m_bankingMix = 0.8f;
                    // m_bankingTimescale = 1;
                    // m_referenceFrame = Quaternion.Identity;
                    m_flags &= ~(VehicleFlag.HOVER_TERRAIN_ONLY | VehicleFlag.LIMIT_ROLL_ONLY | 
                    		VehicleFlag.HOVER_GLOBAL_HEIGHT | VehicleFlag.HOVER_UP_ONLY);
                    m_flags |= (VehicleFlag.NO_DEFLECTION_UP | VehicleFlag.HOVER_WATER_ONLY |
                                VehicleFlag.LIMIT_MOTOR_UP);
                    break;
                case Vehicle.TYPE_AIRPLANE:
                    m_linearFrictionTimescale = new Vector3(200, 10, 5);
                    m_angularFrictionTimescale = new Vector3(20, 20, 20);
                    m_linearMotorDirection = Vector3.Zero;
                    m_linearMotorTimescale = 2;
                    m_linearMotorDecayTimescale = 60;
                    m_angularMotorDirection = Vector3.Zero;
                    m_angularMotorTimescale = 4;
                    m_angularMotorDecayTimescale = 4;
                    m_VhoverHeight = 0;
//                    m_VhoverEfficiency = 0.5f;
                    m_VhoverTimescale = 1000;
                    m_VehicleBuoyancy = 0;
                    // m_linearDeflectionEfficiency = 0.5f;
                    // m_linearDeflectionTimescale = 3;
                    // m_angularDeflectionEfficiency = 1;
                    // m_angularDeflectionTimescale = 2;
                    m_verticalAttractionEfficiency = 0.9f;
                    m_verticalAttractionTimescale = 2f;
                    // m_bankingEfficiency = 1;
                    // m_bankingMix = 0.7f;
                    // m_bankingTimescale = 2;
                    // m_referenceFrame = Quaternion.Identity;
                    m_flags &= ~(VehicleFlag.NO_DEFLECTION_UP | VehicleFlag.HOVER_WATER_ONLY | VehicleFlag.HOVER_TERRAIN_ONLY |
                        VehicleFlag.HOVER_GLOBAL_HEIGHT | VehicleFlag.HOVER_UP_ONLY | VehicleFlag.LIMIT_MOTOR_UP);
                    m_flags |= (VehicleFlag.LIMIT_ROLL_ONLY);
                    break;
                case Vehicle.TYPE_BALLOON:
                    m_linearFrictionTimescale = new Vector3(5, 5, 5);
                    m_angularFrictionTimescale = new Vector3(10, 10, 10);
                    m_linearMotorDirection = Vector3.Zero;
                    m_linearMotorTimescale = 5;
                    m_linearMotorDecayTimescale = 60;
                    m_angularMotorDirection = Vector3.Zero;
                    m_angularMotorTimescale = 6;
                    m_angularMotorDecayTimescale = 10;
                    m_VhoverHeight = 5;
//                    m_VhoverEfficiency = 0.8f;
                    m_VhoverTimescale = 10;
                    m_VehicleBuoyancy = 1;
                    // m_linearDeflectionEfficiency = 0;
                    // m_linearDeflectionTimescale = 5;
                    // m_angularDeflectionEfficiency = 0;
                    // m_angularDeflectionTimescale = 5;
                    m_verticalAttractionEfficiency = 1f;
                    m_verticalAttractionTimescale = 100f;
                    // m_bankingEfficiency = 0;
                    // m_bankingMix = 0.7f;
                    // m_bankingTimescale = 5;
                    // m_referenceFrame = Quaternion.Identity;
                    m_flags &= ~(VehicleFlag.NO_DEFLECTION_UP | VehicleFlag.HOVER_WATER_ONLY | VehicleFlag.HOVER_TERRAIN_ONLY |
                        VehicleFlag.HOVER_UP_ONLY | VehicleFlag.LIMIT_MOTOR_UP);
                    m_flags |= (VehicleFlag.LIMIT_ROLL_ONLY | VehicleFlag.HOVER_GLOBAL_HEIGHT);
                    break;

            }
        }//end SetDefaultsForType

        internal void Enable(IntPtr pBody, OdeScene pParentScene)
        {
            if (m_type == Vehicle.TYPE_NONE)
                return;

            m_body = pBody;
        }

        internal void Step(float pTimestep,  OdeScene pParentScene)
        {
            if (m_body == IntPtr.Zero || m_type == Vehicle.TYPE_NONE)
                return;
            frcount++;					// used to limit debug comment output
            if (frcount > 100)
                frcount = 0;

  			MoveLinear(pTimestep, pParentScene);
            MoveAngular(pTimestep);
        }// end Step

        private void MoveLinear(float pTimestep, OdeScene _pParentScene)
        {
            if (!m_linearMotorDirection.ApproxEquals(Vector3.Zero, 0.01f))		// requested m_linearMotorDirection is significant
            {
            	 if(!d.BodyIsEnabled (Body))  d.BodyEnable (Body);

                // add drive to body
                Vector3 addAmount = m_linearMotorDirection/(m_linearMotorTimescale/pTimestep);
                m_lastLinearVelocityVector += (addAmount*10);					// lastLinearVelocityVector is the current body velocity vector?
        
                // This will work temporarily, but we really need to compare speed on an axis
                // KF: Limit body velocity to applied velocity?
                if (Math.Abs(m_lastLinearVelocityVector.X) > Math.Abs(m_linearMotorDirectionLASTSET.X))
                    m_lastLinearVelocityVector.X = m_linearMotorDirectionLASTSET.X;
                if (Math.Abs(m_lastLinearVelocityVector.Y) > Math.Abs(m_linearMotorDirectionLASTSET.Y))
                    m_lastLinearVelocityVector.Y = m_linearMotorDirectionLASTSET.Y;
                if (Math.Abs(m_lastLinearVelocityVector.Z) > Math.Abs(m_linearMotorDirectionLASTSET.Z))
                    m_lastLinearVelocityVector.Z = m_linearMotorDirectionLASTSET.Z;
                
                // decay applied velocity
                Vector3 decayfraction = ((Vector3.One/(m_linearMotorDecayTimescale/pTimestep)));
                //Console.WriteLine("decay: " + decayfraction);
                m_linearMotorDirection -= m_linearMotorDirection * decayfraction * 0.5f;
                //Console.WriteLine("actual: " + m_linearMotorDirection);
            }
            else
            {		// requested is not significant
					// if what remains of applied is small, zero it.
				if (m_lastLinearVelocityVector.ApproxEquals(Vector3.Zero, 0.01f))
					m_lastLinearVelocityVector = Vector3.Zero;
			}
			            
			
			// convert requested object velocity to world-referenced vector
            m_dir = m_lastLinearVelocityVector;
            d.Quaternion rot = d.BodyGetQuaternion(Body);
	        Quaternion rotq = new Quaternion(rot.X, rot.Y, rot.Z, rot.W);	// rotq = rotation of object
	        m_dir *= rotq;							// apply obj rotation to velocity vector

			// add Gravity andBuoyancy
			// KF: So far I have found no good method to combine a script-requested
			// .Z velocity and gravity. Therefore only 0g will used script-requested
			// .Z velocity. >0g (m_VehicleBuoyancy < 1) will used modified gravity only.
            Vector3 grav = Vector3.Zero;
			if(m_VehicleBuoyancy < 1.0f)
			{
				// There is some gravity, make a gravity force vector
				// that is applied after object velocity.     
	            d.Mass objMass;
	            d.BodyGetMass(Body, out objMass);
	            // m_VehicleBuoyancy: -1=2g; 0=1g; 1=0g; 
	            grav.Z = _pParentScene.gravityz * objMass.mass * (1f - m_VehicleBuoyancy);
	            // Preserve the current Z velocity
				d.Vector3 vel_now = d.BodyGetLinearVel(Body);
	            m_dir.Z = vel_now.Z;		// Preserve the accumulated falling velocity
	        } // else its 1.0, no gravity.
	        
	        // Check if hovering
	        if( (m_flags & (VehicleFlag.HOVER_WATER_ONLY | VehicleFlag.HOVER_TERRAIN_ONLY | VehicleFlag.HOVER_GLOBAL_HEIGHT)) != 0)
	        {	
	        	// We should hover, get the target height
        		d.Vector3 pos = d.BodyGetPosition(Body);
	        	if((m_flags & VehicleFlag.HOVER_WATER_ONLY) == VehicleFlag.HOVER_WATER_ONLY)
	        	{
	        		m_VhoverTargetHeight = _pParentScene.GetWaterLevel() + m_VhoverHeight;
	        	}
	        	else if((m_flags & VehicleFlag.HOVER_TERRAIN_ONLY) == VehicleFlag.HOVER_TERRAIN_ONLY)
	        	{
	        		m_VhoverTargetHeight = _pParentScene.GetTerrainHeightAtXY(pos.X, pos.Y) + m_VhoverHeight;
	        	}
	        	else if((m_flags & VehicleFlag.HOVER_GLOBAL_HEIGHT) == VehicleFlag.HOVER_GLOBAL_HEIGHT)
	        	{
	        		m_VhoverTargetHeight = m_VhoverHeight;
	        	}
	        	
				if((m_flags & VehicleFlag.HOVER_UP_ONLY) == VehicleFlag.HOVER_UP_ONLY)
				{
					// If body is aready heigher, use its height as target height
					if(pos.Z > m_VhoverTargetHeight) m_VhoverTargetHeight = pos.Z;
				}
				
//	            m_VhoverEfficiency = 0f;	// 0=boucy, 1=Crit.damped
//				m_VhoverTimescale = 0f;		// time to acheive height
//				pTimestep  is time since last frame,in secs 
				float herr0 = pos.Z - m_VhoverTargetHeight;
				// Replace Vertical speed with correction figure if significant
				if(Math.Abs(herr0) > 0.01f )
				{
		            d.Mass objMass;
		            d.BodyGetMass(Body, out objMass);
					m_dir.Z = - ( (herr0 * pTimestep * 50.0f) / m_VhoverTimescale);
					//KF: m_VhoverEfficiency is not yet implemented
				}
				else
				{
					m_dir.Z = 0f;
				}
			}
	        	
	        // Apply velocity
	        d.BodySetLinearVel(Body, m_dir.X, m_dir.Y, m_dir.Z);        
            // apply gravity force
			d.BodyAddForce(Body, grav.X, grav.Y, grav.Z);		


			// apply friction
            Vector3 decayamount = Vector3.One / (m_linearFrictionTimescale / pTimestep);
            m_lastLinearVelocityVector -= m_lastLinearVelocityVector * decayamount;
        } // end MoveLinear()
        
        private void MoveAngular(float pTimestep)
        {
	        /*
	        private Vector3 m_angularMotorDirection = Vector3.Zero;			// angular velocity requested by LSL motor 
	        private int m_angularMotorApply = 0;							// application frame counter
 	        private float m_angularMotorVelocity = 0;						// current angular motor velocity (ramps up and down) 
	        private float m_angularMotorTimescale = 0;						// motor angular velocity ramp up rate
	        private float m_angularMotorDecayTimescale = 0;					// motor angular velocity decay rate
	        private Vector3 m_angularFrictionTimescale = Vector3.Zero;		// body angular velocity  decay rate
	        private Vector3 m_lastAngularVelocity = Vector3.Zero;			// what was last applied to body
			*/
        
        	// Get what the body is doing, this includes 'external' influences
        	d.Vector3 angularVelocity = d.BodyGetAngularVel(Body);
   //     	Vector3 angularVelocity = Vector3.Zero;
        	
        	if (m_angularMotorApply > 0)
        	{	
				// ramp up to new value
				//   current velocity  += 		                error       				/    ( time to get there / step interval )
				//							   requested speed     	   -  last motor speed
				m_angularMotorVelocity.X += (m_angularMotorDirection.X - m_angularMotorVelocity.X) /  (m_angularMotorTimescale / pTimestep);
				m_angularMotorVelocity.Y += (m_angularMotorDirection.Y - m_angularMotorVelocity.Y) /  (m_angularMotorTimescale / pTimestep);
				m_angularMotorVelocity.Z += (m_angularMotorDirection.Z - m_angularMotorVelocity.Z) /  (m_angularMotorTimescale / pTimestep);

				m_angularMotorApply--;		// This is done so that if script request rate is less than phys frame rate the expected
											// velocity may still be acheived.
			}
			else
			{
				// no motor recently applied, keep the body velocity
		/*		m_angularMotorVelocity.X = angularVelocity.X;
				m_angularMotorVelocity.Y = angularVelocity.Y;
				m_angularMotorVelocity.Z = angularVelocity.Z; */
				
				// and decay the velocity
				m_angularMotorVelocity -= m_angularMotorVelocity /  (m_angularMotorDecayTimescale / pTimestep);
			} // end motor section
			

            // Vertical attractor section
			Vector3 vertattr = Vector3.Zero;
            
			if(m_verticalAttractionTimescale < 300)
			{
	            float VAservo = 0.2f / (m_verticalAttractionTimescale * pTimestep);
	    	    // get present body rotation
	    	    d.Quaternion rot = d.BodyGetQuaternion(Body);
	    	    Quaternion rotq = new Quaternion(rot.X, rot.Y, rot.Z, rot.W);
	    	    // make a vector pointing up
				Vector3 verterr = Vector3.Zero;
				verterr.Z = 1.0f;
				// rotate it to Body Angle
				verterr = verterr * rotq;
				// verterr.X and .Y are the World error ammounts. They are 0 when there is no error (Vehicle Body is 'vertical'), and .Z will be 1.
				// As the body leans to its side |.X| will increase to 1 and .Z fall to 0. As body inverts |.X| will fall and .Z will go
				// negative. Similar for tilt and |.Y|. .X and .Y must be modulated to prevent a stable inverted body.
				if (verterr.Z < 0.0f)
				{
					verterr.X = 2.0f - verterr.X;
					verterr.Y = 2.0f - verterr.Y;
				}
				// Error is 0 (no error) to +/- 2 (max error)
				// scale it by VAservo
				verterr = verterr * VAservo;
//if(frcount == 0) Console.WriteLine("VAerr=" + verterr);	

				// As the body rotates around the X axis, then verterr.Y increases; Rotated around Y then .X increases, so 
				// Change  Body angular velocity  X based on Y, and Y based on X. Z is not changed.
				vertattr.X =    verterr.Y;
				vertattr.Y =  - verterr.X;
				vertattr.Z = 0f;
				
									// scaling appears better usingsquare-law
				float bounce = 1.0f - (m_verticalAttractionEfficiency * m_verticalAttractionEfficiency);  
				vertattr.X += bounce * angularVelocity.X;
				vertattr.Y += bounce * angularVelocity.Y;
				
			} // else vertical attractor is off
			
	//		m_lastVertAttractor = vertattr;
				
			// Bank section tba
			// Deflection section tba
			
			// Sum velocities
			m_lastAngularVelocity = m_angularMotorVelocity + vertattr; // + bank + deflection
			
        	if (!m_lastAngularVelocity.ApproxEquals(Vector3.Zero, 0.01f))
            {
				if(!d.BodyIsEnabled (Body))  d.BodyEnable (Body);
			}
			else
			{
				m_lastAngularVelocity = Vector3.Zero; // Reduce small value to zero.
			}
			
 			// apply friction
            Vector3 decayamount = Vector3.One / (m_angularFrictionTimescale / pTimestep);
	        m_lastAngularVelocity -= m_lastAngularVelocity * decayamount;   	
	        		
			// Apply to the body
			d.BodySetAngularVel (Body, m_lastAngularVelocity.X, m_lastAngularVelocity.Y, m_lastAngularVelocity.Z);
				
	    } //end MoveAngular
	}
}
