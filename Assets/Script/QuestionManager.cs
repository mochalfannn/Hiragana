using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;

public class QuizManager : MonoBehaviour
{
    [Header("Data & UI")]
    public List<QuestionData> questionBank; 
    public TextMeshProUGUI questionText;
    public Image displayImage; // Pastikan ini diisi di Inspector
    public Button[] answerButtons;
    public TextMeshProUGUI scoreText;

    [Header("Settings")]
    public Color correctColor = Color.green;
    public Color wrongColor = Color.red;
    public float delayBeforeNext = 1.0f;

    private QuestionData currentQuestion;
    private int score = 0;
    private int questionIndex = 0;
    private Color defaultColor;
    private bool isAnswering = false;

    void Start()
    {
        // Validasi Awal
        if (questionBank == null || questionBank.Count == 0) Debug.LogError("Question Bank kosong!");
        if (answerButtons == null || answerButtons.Length == 0) Debug.LogError("Buttons belum dimasukkan!");
        
        // Simpan warna asli tombol dari tombol pertama
        if(answerButtons.Length > 0)
            defaultColor = answerButtons[0].GetComponent<Image>().color;

        ShuffleQuestions();
        DisplayQuestion();
    }

    void ShuffleQuestions()
    {
        for (int i = 0; i < questionBank.Count; i++)
        {
            QuestionData temp = questionBank[i];
            int randomIndex = Random.Range(i, questionBank.Count);
            questionBank[i] = questionBank[randomIndex];
            questionBank[randomIndex] = temp;
        }
    }

    void DisplayQuestion()
    {
        if (questionIndex < questionBank.Count)
        {
            isAnswering = false;
            currentQuestion = questionBank[questionIndex];

            // 1. Tampilkan Teks Pertanyaan
            if (questionText != null) 
                questionText.text = currentQuestion.questionText;

            // 2. Tampilkan Gambar (Aktifkan jika ada, sembunyikan jika tidak ada)
            if (displayImage != null)
            {
                if (currentQuestion.questionImage != null)
                {
                    displayImage.gameObject.SetActive(true);
                    displayImage.sprite = currentQuestion.questionImage;
                }
                else
                {
                    displayImage.gameObject.SetActive(false);
                }
            }

            // 3. Atur Tombol Jawaban
            for (int i = 0; i < answerButtons.Length; i++)
            {
                // Reset Warna
                Image btnImage = answerButtons[i].GetComponent<Image>();
                if (btnImage != null) btnImage.color = defaultColor;

                // Isi Teks Tombol
                TextMeshProUGUI btnText = answerButtons[i].GetComponentInChildren<TextMeshProUGUI>();
                if (btnText != null && i < currentQuestion.answers.Length)
                {
                    btnText.text = currentQuestion.answers[i];
                }

                // Set Click Listener
                int index = i; 
                answerButtons[i].onClick.RemoveAllListeners();
                answerButtons[i].onClick.AddListener(() => StartCoroutine(HandleAnswerSelection(index)));
            }
        }
        else
        {
            // Kuis Selesai
            questionText.text = "Kuis Selesai!";
        }
    }

    IEnumerator HandleAnswerSelection(int selectedIndex)
    {
        if (isAnswering) yield break; 
        isAnswering = true;

        // Visual Feedback
        if (selectedIndex == currentQuestion.correctAnswerIndex)
        {
            score += 10;
            answerButtons[selectedIndex].GetComponent<Image>().color = correctColor;
        }
        else
        {
            answerButtons[selectedIndex].GetComponent<Image>().color = wrongColor;
            // Tunjukkan jawaban yang benar
            answerButtons[currentQuestion.correctAnswerIndex].GetComponent<Image>().color = correctColor;
        }

        if (scoreText != null) scoreText.text = "Score: " + score;
        
        yield return new WaitForSeconds(delayBeforeNext);

        questionIndex++;
        DisplayQuestion();
    }
}