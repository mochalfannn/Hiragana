using UnityEngine;

[CreateAssetMenu(fileName = "New Question", menuName = "Quiz/Question")]
public class QuestionData : ScriptableObject
{
    [TextArea(3, 10)]
    public string questionText;
    public Sprite questionImage;
    public string[] answers;
    public int correctAnswerIndex;
}