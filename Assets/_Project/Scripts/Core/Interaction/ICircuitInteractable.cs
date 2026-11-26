namespace CircuitLab.Core.Interaction
{
    /// <summary>
    /// XR'dan mustaqil o'zaro ta'sir shartnomasi. AR (touch), VR (gaze/kontroller) yoki
    /// Editor (sichqoncha) kontrollerlari obyektlar bilan faqat shu interfeys orqali ishlaydi.
    /// </summary>
    public interface ICircuitInteractable
    {
        /// <summary>Hozir tanlash mumkinmi (masalan, kuygan lampochka — yo'q).</summary>
        bool IsInteractable { get; }

        /// <summary>Kursor/nigoh obyektga tushdi.</summary>
        void OnHoverEnter();

        /// <summary>Kursor/nigoh obyektdan chiqdi.</summary>
        void OnHoverExit();

        /// <summary>Obyekt tanlandi (tap, dwell yoki trigger).</summary>
        void OnSelect();
    }
}
