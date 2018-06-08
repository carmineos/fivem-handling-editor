using CitizenFX.Core;
using System.Collections.Generic;

namespace HandlingEditor
{
    // https://www.gtamodding.com/wiki/Handling.meta
    public class CHandlingData
    {
        public string handlingName;
        public float fMass;
        public float fInitialDragCoeff;
        public float fPercentSubmerged;
        public Vector3 vecCentreOfMassOffset;
        public Vector3 vecInertiaMultiplier;
        public float fDriveBiasFront;
        public int nInitialDriveGears;
        public float fInitialDriveForce;
        public float fDriveInertia;
        public float fClutchChangeRateScaleUpShift;
        public float fClutchChangeRateScaleDownShift;
        public float fInitialDriveMaxFlatVel;
        public float fBrakeForce;
        public float fBrakeBiasFront;
        public float fHandBrakeForce;
        public float fSteeringLock;
        public float fTractionCurveMax;
        public float fTractionCurveMin;
        public float fTractionCurveLateral;
        public float fTractionSpringDeltaMax;
        public float fLowSpeedTractionLossMult;
        public float fCamberStiffnesss;
        public float fTractionBiasFront;
        public float fTractionLossMult;
        public float fSuspensionForce;
        public float fSuspensionCompDamp;
        public float fSuspensionReboundDamp;
        public float fSuspensionUpperLimit;
        public float fSuspensionLowerLimit;
        public float fSuspensionRaise;
        public float fSuspensionBiasFront;
        public float fAntiRollBarForce;
        public float fAntiRollBarBiasFront;
        public float fRollCentreHeightFront;
        public float fRollCentreHeightRear;
        public float fCollisionDamageMult;
        public float fWeaponDamageMult;
        public float fDeformationDamageMult;
        public float fEngineDamageMult;
        public float fPetrolTankVolume;
        public float fOilVolume;
        public float fSeatOffsetDistX;
        public float fSeatOffsetDistY;
        public float fSeatOffsetDistZ;
        public int nMonetaryValue;
        public string strModelFlags;
        public string strHandlingFlags;
        public string strDamageFlags;
        public string AIHandling;
        public SubHandlingData SubHandlingData;
        public float fWeaponDamageScaledToVehHealthMult;
        public float fPopUpLightRotation;
        public float fDownforceModifier;
        public float fRocketBoostCapacity;
        public float fBoostMaxSpeed;
    }

    public class SubHandlingData
    {
        public List<CBaseSubHandlingData> Items;
    }

    public abstract class CBaseSubHandlingData
    {
    }
}
