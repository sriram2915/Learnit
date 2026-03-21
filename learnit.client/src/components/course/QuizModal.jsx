import { useState, useEffect } from "react";
import { FaTimes, FaCheck, FaClock } from "react-icons/fa";
import Modal from "../ui/Modal";
import Button from "../ui/Button";
import { quizApi } from "../../services";
import styles from "./QuizModal.module.css";

function QuizModal({
  moduleId,
  moduleTitle,
  isOpen,
  onClose,
  onQuizPassed,
  allowRetake = false,
}) {
  const [quiz, setQuiz] = useState(null);
  const [loading, setLoading] = useState(false);
  const [submitting, setSubmitting] = useState(false);
  const [answers, setAnswers] = useState({});
  const [currentQuestionIndex, setCurrentQuestionIndex] = useState(0);
  const [startTime] = useState(Date.now());
  const [result, setResult] = useState(null);
  const [error, setError] = useState("");

  useEffect(() => {
    if (isOpen && moduleId) {
      loadQuiz();
    } else {
      // Reset state when modal closes
      setQuiz(null);
      setAnswers({});
      setCurrentQuestionIndex(0);
      setResult(null);
      setError("");
    }
  }, [isOpen, moduleId]);

  const loadQuiz = async () => {
    if (!moduleId) {
      setError("Module ID is required to load quiz.");
      return;
    }

    setLoading(true);
    setError("");
    try {
      const quizData = await quizApi.getQuizForModule(moduleId);
      setQuiz(quizData);
      setCurrentQuestionIndex(0);
    } catch (err) {
      setError(err.message || "Failed to load quiz. Please try again.");
      console.error("Failed to load quiz:", err);
    } finally {
      setLoading(false);
    }
  };

  const handleAnswerSelect = (questionId, optionId) => {
    setAnswers((prev) => ({
      ...prev,
      [questionId]: optionId,
    }));
  };

  const handleNext = () => {
    if (currentQuestionIndex < quiz.questions.length - 1) {
      setCurrentQuestionIndex(currentQuestionIndex + 1);
    }
  };

  const handlePrevious = () => {
    if (currentQuestionIndex > 0) {
      setCurrentQuestionIndex(currentQuestionIndex - 1);
    }
  };

  const handleSubmit = async () => {
    if (!quiz) return;

    // Check if all questions are answered
    const unanswered = quiz.questions.filter((q) => !answers[q.id]);
    if (unanswered.length > 0) {
      setError(
        `Please answer all questions. ${unanswered.length} question(s) remaining.`
      );
      return;
    }

    setSubmitting(true);
    setError("");

    try {
      const timeSpent = Math.floor((Date.now() - startTime) / 1000);
      const submission = {
        answers: quiz.questions.map((q) => ({
          questionId: q.id,
          selectedOptionId: answers[q.id],
        })),
        timeSpentSeconds: timeSpent,
      };

      const result = await quizApi.submitQuizAttempt(quiz.id, submission);
      setResult(result);

      if (result.passed) {
        // Wait a moment to show success, then close and refresh
        setTimeout(() => {
          onQuizPassed();
          onClose();
        }, 2000);
      }
    } catch (err) {
      setError(err.message || "Failed to submit quiz. Please try again.");
      console.error("Failed to submit quiz:", err);
    } finally {
      setSubmitting(false);
    }
  };

  if (!isOpen) return null;

  const currentQuestion = quiz?.questions[currentQuestionIndex];
  const progress = quiz
    ? ((currentQuestionIndex + 1) / quiz.questions.length) * 100
    : 0;
  const allAnswered = quiz ? quiz.questions.every((q) => answers[q.id]) : false;

  return (
    <Modal title={`Module Quiz: ${moduleTitle}`} onClose={onClose}>
      <div className={styles.quizModal}>
        {loading && (
          <div className={styles.loading}>
            <p>Loading quiz...</p>
          </div>
        )}

        {error && !loading && (
          <div className={styles.error}>
            <p>{error}</p>
            <Button onClick={loadQuiz}>Retry</Button>
          </div>
        )}

        {quiz && !result && !loading && (
          <>
            <div className={styles.progress}>
              <div className={styles.progressBar}>
                <div
                  className={styles.progressFill}
                  style={{ width: `${progress}%` }}
                />
              </div>
              <span className={styles.progressText}>
                Question {currentQuestionIndex + 1} of {quiz.questions.length}
              </span>
            </div>

            {currentQuestion && (
              <div className={styles.question}>
                <h3 className={styles.questionText}>
                  {currentQuestion.questionText}
                </h3>
                <div className={styles.options}>
                  {currentQuestion.options.map((option) => (
                    <label
                      key={option.id}
                      className={`${styles.option} ${
                        answers[currentQuestion.id] === option.id
                          ? styles.selected
                          : ""
                      }`}
                    >
                      <input
                        type="radio"
                        name={`question-${currentQuestion.id}`}
                        value={option.id}
                        checked={answers[currentQuestion.id] === option.id}
                        onChange={() =>
                          handleAnswerSelect(currentQuestion.id, option.id)
                        }
                      />
                      <span>{option.optionText}</span>
                    </label>
                  ))}
                </div>
              </div>
            )}

            <div className={styles.navigation}>
              <Button
                onClick={handlePrevious}
                disabled={currentQuestionIndex === 0}
                variant="secondary"
              >
                Previous
              </Button>
              <div className={styles.questionIndicators}>
                {quiz.questions.map((q, idx) => (
                  <button
                    key={q.id}
                    className={`${styles.indicator} ${
                      answers[q.id] ? styles.answered : ""
                    } ${idx === currentQuestionIndex ? styles.current : ""}`}
                    onClick={() => setCurrentQuestionIndex(idx)}
                    title={q.questionText.substring(0, 50)}
                  >
                    {idx + 1}
                  </button>
                ))}
              </div>
              {currentQuestionIndex < quiz.questions.length - 1 ? (
                <Button onClick={handleNext} variant="primary">
                  Next
                </Button>
              ) : (
                <Button
                  onClick={handleSubmit}
                  disabled={!allAnswered || submitting}
                  variant="primary"
                >
                  {submitting ? "Submitting..." : "Submit Quiz"}
                </Button>
              )}
            </div>
          </>
        )}

        {result && (
          <div className={styles.result}>
            <div
              className={`${styles.resultHeader} ${
                result.passed ? styles.passed : styles.failed
              }`}
            >
              {result.passed ? (
                <>
                  <FaCheck className={styles.resultIcon} />
                  <h2>Quiz Passed!</h2>
                </>
              ) : (
                <>
                  <FaTimes className={styles.resultIcon} />
                  <h2>Quiz Not Passed</h2>
                </>
              )}
            </div>
            <div className={styles.resultStats}>
              <div className={styles.stat}>
                <span className={styles.statLabel}>Score:</span>
                <span className={styles.statValue}>{result.score}%</span>
              </div>
              <div className={styles.stat}>
                <span className={styles.statLabel}>Correct:</span>
                <span className={styles.statValue}>
                  {result.correctAnswers} / {result.totalQuestions}
                </span>
              </div>
              <div className={styles.stat}>
                <span className={styles.statLabel}>Passing Score:</span>
                <span className={styles.statValue}>70%</span>
              </div>
            </div>
            {!result.passed && (
              <div className={styles.retake}>
                <p>You need at least 70% to pass.</p>
                {allowRetake && (
                  <Button onClick={loadQuiz} variant="primary">
                    Retake Quiz
                  </Button>
                )}
              </div>
            )}
            {result.passed && (
              <div className={styles.success}>
                <p>Congratulations! Module marked as completed.</p>
              </div>
            )}
          </div>
        )}
      </div>
    </Modal>
  );
}

export default QuizModal;
