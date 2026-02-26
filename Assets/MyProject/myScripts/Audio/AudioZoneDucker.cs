using System.Collections.Generic;
using UnityEngine;

namespace MyProject.Audio
{
    /// Παρακολουθεί όλα τα ProximityAudioZone instances και κάνει duck τον ambient ήχο
    /// βάσει της πιο δυνατής κοντινής ζώνης.
    ///
    /// Όταν ο παίκτης εισέρχεται σε μια δυνατή ζώνη ήχου (όπως αγορά ή καταρράκτης),
    /// αυτό το script μειώνει την ένταση του ambient υπόβαθρου ώστε ο ήχος της ζώνης
    /// να ακούγεται καθαρά χωρίς να γίνουν όλα πολύ δυνατά.
    ///
    /// Ρύθμιση:
    /// 1. Προσθέστε αυτό το script στο ίδιο GameObject με τον AmbientSoundManager
    /// 2. Αυτό είναι όλο! Οι ζώνες βρίσκονται αυτόματα στο Start()
    ///
    /// Πώς λειτουργεί:
    /// 1. Βρίσκει όλα τα ProximityAudioZone instances στη σκηνή
    /// 2. Κάθε frame, παίρνει το CurrentVolumeRatio από κάθε ζώνη
    /// 3. Παίρνει το υψηλότερο ratio (η πιο δυνατή κοντινή ζώνη)
    /// 4. Λέει στον AmbientSoundManager να κάνει duck κατά αυτό το ποσό
    public class AudioZoneDucker : MonoBehaviour
    {

        [Header("Zone Detection")]

        /// Λίστα με όλα τα ProximityAudioZones στη σκηνή.
        /// Συμπληρώνεται αυτόματα στο Start(), αλλά μπορεί να ανατεθεί χειροκίνητα αν χρειαστεί.
        [Tooltip("Όλες οι ζώνες ήχου προς παρακολούθηση. Συμπληρώνεται αυτόματα στο Start().")]
        [SerializeField]
        private List<ProximityAudioZone> audioZones = new List<ProximityAudioZone>();

        [Header("Ducking Settings")]

        /// Πολλαπλασιαστής που εφαρμόζεται στο duck amount.
        /// 1.0 = ζώνη σε πλήρη ένταση προκαλεί πλήρες ducking
        /// 0.5 = ζώνη σε πλήρη ένταση προκαλεί μισό ducking
        /// Ρυθμίστε για να καθορίσετε πόσο επηρεάζουν οι ζώνες τον ambient ήχο.
        [Tooltip("Πολλαπλασιαστής για duck amount. Χαμηλότερο = λιγότερο επιθετικό ducking.")]
        [Range(0f, 1f)]
        [SerializeField]
        private float duckingIntensity = 0.8f;

        /// Αν είναι true, βρίσκει αυτόματα όλα τα ProximityAudioZones στο Start().
        /// Αν είναι false, πρέπει να αναθέσετε χειροκίνητα τις ζώνες στον Inspector.
        [Tooltip("Αυτόματη εύρεση όλων των ProximityAudioZones στη σκηνή κατά το Start.")]
        [SerializeField]
        private bool autoFindZones = true;

        /// Το υψηλότερο volume ratio μεταξύ όλων των ζωνών αυτό το frame.
        /// Χρησιμοποιείται για debugging και παρακολούθηση.
        private float currentHighestRatio;

        private void Start()
        {
            // Αυτόματη εύρεση όλων των ProximityAudioZones στη σκηνή
            if (autoFindZones)
            {
                FindAllAudioZones();
            }

            // Επικύρωση ότι έχουμε ζώνες προς παρακολούθηση
            if (audioZones.Count == 0)
            {
                Debug.LogWarning("[AudioZoneDucker] Δεν βρέθηκαν ProximityAudioZones στη σκηνή. " +
                                "Δεν θα γίνει ducking μέχρι να προστεθούν ζώνες.");
            }
            else
            {
                Debug.Log($"[AudioZoneDucker] Βρέθηκαν {audioZones.Count} ζώνη/ες ήχου προς παρακολούθηση.");
            }

            // Επικύρωση ότι υπάρχει ο AmbientSoundManager
            if (AmbientSoundManager.Instance == null)
            {
                Debug.LogWarning("[AudioZoneDucker] Δεν βρέθηκε AmbientSoundManager. " +
                                "Βεβαιωθείτε ότι είναι στη σκηνή και αρχικοποιείται πριν από αυτό το script.");
            }
        }

        private void Update()
        {
            // Παράλειψη αν δεν υπάρχουν ζώνες προς παρακολούθηση
            if (audioZones.Count == 0)
            {
                return;
            }

            // Εύρεση του υψηλότερου volume ratio μεταξύ όλων των ζωνών
            // Αυτό αντιπροσωπεύει την "πιο δυνατή" κοντινή ζώνη
            float highestRatio = 0f;

            for (int i = 0; i < audioZones.Count; i++)
            {
                ProximityAudioZone zone = audioZones[i];

                // Παράλειψη null ζωνών (μπορεί να έχουν καταστραφεί)
                if (zone == null)
                {
                    continue;
                }

                // Λήψη του τρέχοντος volume ratio αυτής της ζώνης
                float ratio = zone.CurrentVolumeRatio;

                // Παρακολούθηση του υψηλότερου ratio
                if (ratio > highestRatio)
                {
                    highestRatio = ratio;
                }
            }

            // Αποθήκευση για debugging
            currentHighestRatio = highestRatio;

            // Εφαρμογή πολλαπλασιαστή ducking intensity
            float duckAmount = highestRatio * duckingIntensity;

            // Ενημέρωση του AmbientSoundManager να κάνει duck κατά αυτό το ποσό
            if (AmbientSoundManager.Instance != null)
            {
                AmbientSoundManager.Instance.SetDuckAmount(duckAmount);
            }
        }

        /// Βρίσκει όλα τα ProximityAudioZones στη σκηνή και τα προσθέτει στη λίστα.
        /// Καλείται αυτόματα στο Start() αν το autoFindZones είναι true.
        public void FindAllAudioZones()
        {
            // Καθαρισμός υπάρχουσας λίστας
            audioZones.Clear();

            // Εύρεση όλων των ProximityAudioZone components στη σκηνή
            ProximityAudioZone[] foundZones = FindObjectsByType<ProximityAudioZone>(FindObjectsSortMode.None);

            // Προσθήκη στη λίστα
            audioZones.AddRange(foundZones);

            Debug.Log($"[AudioZoneDucker] Βρέθηκαν και καταχωρήθηκαν {foundZones.Length} ζώνη/ες ήχου.");
        }

        /// Καταχωρεί χειροκίνητα μια νέα ζώνη ήχου.
        /// Χρήσιμο για δυναμικά δημιουργημένες ζώνες.
        /// <param name="zone">Η ζώνη προς καταχώρηση.</param>
        public void RegisterZone(ProximityAudioZone zone)
        {
            if (zone != null && !audioZones.Contains(zone))
            {
                audioZones.Add(zone);
                Debug.Log($"[AudioZoneDucker] Καταχωρήθηκε νέα ζώνη: {zone.gameObject.name}");
            }
        }

        /// Αποκαταχωρεί χειροκίνητα μια ζώνη ήχου.
        /// Καλέστε αυτό πριν καταστρέψετε μια ζώνη για να κρατήσετε τη λίστα καθαρή.
        /// <param name="zone">Η ζώνη προς αποκαταχώρηση.</param>
        public void UnregisterZone(ProximityAudioZone zone)
        {
            if (audioZones.Contains(zone))
            {
                audioZones.Remove(zone);
                Debug.Log($"[AudioZoneDucker] Αποκαταχωρήθηκε ζώνη: {zone.gameObject.name}");
            }
        }

        /// Αφαιρεί τυχόν null εγγραφές από τη λίστα ζωνών.
        /// Καλέστε αυτό αν ζώνες έχουν καταστραφεί χωρίς αποκαταχώρηση.
        public void CleanupNullZones()
        {
            int removed = audioZones.RemoveAll(zone => zone == null);
            if (removed > 0)
            {
                Debug.Log($"[AudioZoneDucker] Καθαρίστηκαν {removed} null αναφορά/ές ζωνών.");
            }
        }

        /// Επιστρέφει το τρέχον υψηλότερο volume ratio μεταξύ όλων των ζωνών.
        /// Χρήσιμο για debugging ή εμφάνιση UI.
        public float CurrentHighestRatio => currentHighestRatio;

        /// Επιστρέφει τον αριθμό των καταχωρημένων ζωνών ήχου.
        public int ZoneCount => audioZones.Count;
    }
}
