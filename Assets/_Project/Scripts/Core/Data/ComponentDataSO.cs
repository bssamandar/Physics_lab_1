using System;
using UnityEngine;

namespace CircuitLab.Core
{
    /// <summary>Laboratoriya komponentining fizik turi.</summary>
    public enum ComponentKind
    {
        /// <summary>Kuchlanish manbai (EYK + ichki qarshilik).</summary>
        Battery = 0,
        /// <summary>Oddiy rezistor.</summary>
        Resistor = 1,
        /// <summary>Lampochka (rezistor + yorug'lik).</summary>
        Bulb = 2,
        /// <summary>Kalit (ochiq/yopiq).</summary>
        Switch = 3,
        /// <summary>Ampermetr (juda kichik shunt qarshilik).</summary>
        Ammeter = 4,
        /// <summary>Voltmetr (juda katta kirish qarshiligi).</summary>
        Voltmeter = 5
    }

    /// <summary>
    /// Komponent parametrlarini (kuchlanish, qarshilik, maksimal tok) saqlovchi ma'lumot aktivi.
    /// Bir xil turdagi barcha prefablar bitta aktivdan foydalanadi (Flyweight).
    /// </summary>
    [CreateAssetMenu(fileName = "CD_NewComponent", menuName = "Circuit Lab/Component Data", order = 0)]
    public sealed class ComponentDataSO : ScriptableObject
    {
        private const float MinPassiveResistance = 1e-3f;
        private const float MinBatteryInternalResistance = 1e-2f;
        private const float MinAmmeterResistance = 1e-4f;
        private const float MinVoltmeterResistance = 1e3f;

        [SerializeField] private string displayName = "Component";
        [SerializeField] private ComponentKind kind = ComponentKind.Resistor;

        [Tooltip("Faqat Battery uchun: EYK, Volt.")]
        [SerializeField, Min(0f)] private float voltage = 4.5f;

        [Tooltip("Om. Battery: ichki qarshilik. Ammeter: shunt. Voltmeter: kirish qarshiligi. Switch: yopiq holatdagi qarshilik.")]
        [SerializeField, Min(0f)] private float resistance = 10f;

        [Tooltip("Amper. Bundan katta tok — ortiqcha yuklanish (Battery uchun — qisqa tutashuv).")]
        [SerializeField, Min(0.001f)] private float maxCurrent = 1f;

        [Tooltip("Vatt. Bulb uchun to'liq yorqinlikka mos nominal quvvat.")]
        [SerializeField, Min(0.001f)] private float ratedPower = 1f;

        [Tooltip("true bo'lsa, maxCurrent oshganda komponent kuyadi (zanjirni uzadi).")]
        [SerializeField] private bool breaksOnOverload;

        /// <summary>Inspector'da parametr o'zgarganda chaqiriladi (runtime'da jonli sozlash uchun).</summary>
        public event Action Changed;

        /// <summary>UI uchun nom.</summary>
        public string DisplayName => displayName;
        /// <summary>Komponent turi.</summary>
        public ComponentKind Kind => kind;
        /// <summary>EYK (V), faqat Battery uchun ma'noli.</summary>
        public float Voltage => voltage;
        /// <summary>Samarali qarshilik (Om), tur bo'yicha minimal qiymat bilan cheklangan.</summary>
        public float Resistance => resistance;
        /// <summary>Ruxsat etilgan maksimal tok (A).</summary>
        public float MaxCurrent => maxCurrent;
        /// <summary>Nominal quvvat (W).</summary>
        public float RatedPower => ratedPower;
        /// <summary>Ortiqcha yuklanishda kuyadimi.</summary>
        public bool BreaksOnOverload => breaksOnOverload;

        private void OnValidate()
        {
            resistance = Mathf.Max(resistance, GetMinResistance(kind));
            if (kind != ComponentKind.Battery) voltage = 0f;
            Changed?.Invoke();
        }

        private static float GetMinResistance(ComponentKind k)
        {
            switch (k)
            {
                case ComponentKind.Battery: return MinBatteryInternalResistance;
                case ComponentKind.Ammeter: return MinAmmeterResistance;
                case ComponentKind.Voltmeter: return MinVoltmeterResistance;
                default: return MinPassiveResistance;
            }
        }
    }
}
