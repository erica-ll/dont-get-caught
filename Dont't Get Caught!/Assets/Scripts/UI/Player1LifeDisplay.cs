using System.Collections;
using DontGetCaught.Core;
using UnityEngine;

namespace DontGetCaught.UI
{
    /// <summary>
    /// Keeps each remaining life visible and briefly shows its matching loss
    /// graphic when that life is consumed.
    /// </summary>
    public sealed class Player1LifeDisplay : MonoBehaviour
    {
        [Header("Game Source")]
        [SerializeField] private GameManager gameManager;

        [Header("Life Objects (ordered Life 1, Life 2, Life 3)")]
        [SerializeField] private GameObject[] lifeIcons;
        [SerializeField] private GameObject[] lifeLossIcons;

        [Header("Display Timing")]
        [SerializeField, Min(0f)] private float lossDisplaySeconds = 1.5f;

        private Coroutine[] hideRoutines;
        private int lastLives;

        private void OnEnable()
        {
            if (gameManager == null)
            {
                gameManager = FindFirstObjectByType<GameManager>();
            }

            hideRoutines = new Coroutine[GetSlotCount()];

            if (gameManager == null)
            {
                Debug.LogError("Player1LifeDisplay needs a GameManager reference.", this);
                return;
            }

            lastLives = gameManager.RemainingLives;
            gameManager.LivesChanged += HandleLivesChanged;
            ShowRemainingLives(lastLives);
        }

        private void OnDisable()
        {
            if (gameManager != null)
            {
                gameManager.LivesChanged -= HandleLivesChanged;
            }

            StopAllCoroutines();
            hideRoutines = null;
        }

        private void OnValidate()
        {
            lossDisplaySeconds = Mathf.Max(0f, lossDisplaySeconds);
        }

        private void HandleLivesChanged(int lives)
        {
            if (lives < lastLives)
            {
                int lostLifeIndex = lastLives - 1;
                ShowLossBriefly(lostLifeIndex);
                UpdateOtherLifeSlots(lives, lostLifeIndex);
            }
            else if (lives > lastLives)
            {
                StopAllCoroutines();
                hideRoutines = new Coroutine[GetSlotCount()];
                ShowRemainingLives(lives);
            }

            lastLives = lives;
        }

        private void ShowLossBriefly(int index)
        {
            if (!IsValidIndex(index))
            {
                ShowRemainingLives(lastLives - 1);
                return;
            }

            if (lifeIcons[index] != null) lifeIcons[index].SetActive(true);
            if (lifeLossIcons[index] != null) lifeLossIcons[index].SetActive(true);

            if (hideRoutines[index] != null)
            {
                StopCoroutine(hideRoutines[index]);
            }

            hideRoutines[index] = StartCoroutine(HideLostLifeAfterDelay(index));
        }

        private IEnumerator HideLostLifeAfterDelay(int index)
        {
            yield return new WaitForSecondsRealtime(lossDisplaySeconds);

            if (IsValidIndex(index))
            {
                if (lifeIcons[index] != null) lifeIcons[index].SetActive(false);
                if (lifeLossIcons[index] != null) lifeLossIcons[index].SetActive(false);
                hideRoutines[index] = null;
            }
        }

        private void UpdateOtherLifeSlots(int remainingLives, int recentlyLostIndex)
        {
            for (int i = 0; i < GetSlotCount(); i++)
            {
                if (i == recentlyLostIndex || (hideRoutines != null && hideRoutines[i] != null))
                {
                    continue;
                }

                if (lifeIcons[i] != null) lifeIcons[i].SetActive(i < remainingLives);
                if (lifeLossIcons[i] != null) lifeLossIcons[i].SetActive(false);
            }
        }

        private void ShowRemainingLives(int lives)
        {
            for (int i = 0; i < GetSlotCount(); i++)
            {
                if (lifeIcons[i] != null) lifeIcons[i].SetActive(i < lives);
                if (lifeLossIcons[i] != null) lifeLossIcons[i].SetActive(false);
            }
        }

        private int GetSlotCount()
        {
            if (lifeIcons == null || lifeLossIcons == null)
            {
                return 0;
            }

            return Mathf.Min(lifeIcons.Length, lifeLossIcons.Length);
        }

        private bool IsValidIndex(int index)
        {
            return index >= 0
                   && index < GetSlotCount()
                   && lifeIcons[index] != null
                   && lifeLossIcons[index] != null;
        }
    }
}
