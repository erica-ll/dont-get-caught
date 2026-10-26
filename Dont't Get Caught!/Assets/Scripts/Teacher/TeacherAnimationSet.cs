using UnityEngine;

namespace DontGetCaught.Teacher
{
    /// <summary>Sprite sequences supplied by the art team for teacher states.</summary>
    [CreateAssetMenu(fileName = "TeacherAnimationSet", menuName = "Don't Get Caught/Teacher Animation Set")]
    public sealed class TeacherAnimationSet : ScriptableObject
    {
        [SerializeField] private Sprite[] walkCirculation;
        [SerializeField] private Sprite[] writingCirculation;
        [SerializeField] private Sprite[] alert;
        [SerializeField] private Sprite[] angryCirculation;
        [SerializeField] private Sprite[] angryTurningAround;
        [SerializeField] private Sprite[] turningAroundToWrite;
        [SerializeField] private Sprite[] turningAround;

        public Sprite[] WalkCirculation => walkCirculation;
        public Sprite[] WritingCirculation => writingCirculation;
        public Sprite[] Alert => alert;
        public Sprite[] AngryCirculation => angryCirculation;
        public Sprite[] AngryTurningAround => angryTurningAround;
        public Sprite[] TurningAroundToWrite => turningAroundToWrite;
        public Sprite[] TurningAround => turningAround;
    }
}
