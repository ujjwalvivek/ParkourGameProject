using UnityEngine;

namespace Michsky.UI.Dark
{
    public class ModalWindowManager : MonoBehaviour
    {
        public static ModalWindowManager Instance;
        
        [Header("BRUSH ANIMATION")]
        public Animator brushAnimator;
        public bool enableSplash = true;

        private Animator mWindowAnimator;

        void Awake()
        {
            Instance = this;
        }

        void Start()
        {
            mWindowAnimator = gameObject.GetComponent<Animator>();
        }

        public void ModalWindowIn()
        {
            mWindowAnimator.Play("Modal Window In");

            if(enableSplash == true)
            {
                brushAnimator.Play("Transition Out");
            }
        }

        public void ModalWindowOut()
        {
            mWindowAnimator.Play("Modal Window Out");

            if (enableSplash == true)
            {
                brushAnimator.Play("Transition In");
            }
        }
    }
}