import { useState, useEffect } from "react";
import Modal from "../ui/Modal";
import Button from "../ui/Button";
import { courseApi } from "../../services";
import { Loading } from "../ui/index";
import styles from "./CourseAssignmentModal.module.css";
import { FaCheck, FaBook } from "react-icons/fa";

function CourseAssignmentModal({ isOpen, onClose, onSubmit }) {
  const [courses, setCourses] = useState([]);
  const [selectedCourseIds, setSelectedCourseIds] = useState([]);
  const [loading, setLoading] = useState(true);
  const [submitting, setSubmitting] = useState(false);
  const [error, setError] = useState("");

  useEffect(() => {
    if (isOpen) {
      fetchCourses();
    } else {
      // Reset when modal closes
      setSelectedCourseIds([]);
      setError("");
    }
  }, [isOpen]);

  const fetchCourses = async () => {
    try {
      setLoading(true);
      setError("");
      const data = await courseApi.getCourses();
      setCourses(data);
    } catch (err) {
      setError(err.message || "Failed to load courses");
    } finally {
      setLoading(false);
    }
  };

  const handleToggleCourse = (courseId) => {
    setSelectedCourseIds((prev) =>
      prev.includes(courseId)
        ? prev.filter((id) => id !== courseId)
        : [...prev, courseId]
    );
  };

  const handleSelectAll = () => {
    if (selectedCourseIds.length === courses.length) {
      setSelectedCourseIds([]);
    } else {
      setSelectedCourseIds(courses.map((c) => c.id));
    }
  };

  const handleSubmit = async (e) => {
    e.preventDefault();
    setError("");

    if (selectedCourseIds.length === 0) {
      setError("Please select at least one course to share");
      return;
    }

    try {
      setSubmitting(true);
      await onSubmit(selectedCourseIds);
      setSelectedCourseIds([]);
    } catch (err) {
      // Error handled by parent
    } finally {
      setSubmitting(false);
    }
  };

  if (!isOpen) return null;

  return (
    <Modal isOpen={isOpen} onClose={onClose} title="Share Courses to Classroom">
      <form onSubmit={handleSubmit} className={styles.form}>
        <div className={styles.info}>
          <p>Select one or more courses to share with the classroom members.</p>
          <p className={styles.hint}>
            Members will be able to copy these courses to their personal library.
          </p>
        </div>

        {error && <div className={styles.error}>{error}</div>}

        {loading ? (
          <Loading />
        ) : courses.length === 0 ? (
          <div className={styles.emptyState}>
            <FaBook className={styles.emptyIcon} />
            <p>You don't have any courses yet.</p>
            <p>Create a course first to share it with classrooms.</p>
          </div>
        ) : (
          <>
            <div className={styles.selectAll}>
              <button
                type="button"
                className={styles.selectAllButton}
                onClick={handleSelectAll}
              >
                {selectedCourseIds.length === courses.length
                  ? "Deselect All"
                  : "Select All"}
              </button>
              <span className={styles.selectedCount}>
                {selectedCourseIds.length} of {courses.length} selected
              </span>
            </div>

            <div className={styles.coursesList}>
              {courses.map((course) => {
                const isSelected = selectedCourseIds.includes(course.id);
                return (
                  <div
                    key={course.id}
                    className={`${styles.courseItem} ${isSelected ? styles.selected : ""}`}
                    onClick={() => handleToggleCourse(course.id)}
                  >
                    <div className={styles.checkbox}>
                      {isSelected && <FaCheck />}
                    </div>
                    <div className={styles.courseInfo}>
                      <h4>{course.title}</h4>
                      <p className={styles.courseDescription}>
                        {course.description || "No description"}
                      </p>
                      <div className={styles.courseMeta}>
                        <span>{course.subjectArea || "General"}</span>
                        <span>•</span>
                        <span>{course.difficulty || "N/A"}</span>
                        <span>•</span>
                        <span>{course.totalEstimatedHours || 0}h</span>
                      </div>
                    </div>
                  </div>
                );
              })}
            </div>
          </>
        )}

        <div className={styles.actions}>
          <Button type="button" variant="secondary" onClick={onClose}>
            Cancel
          </Button>
          <Button
            type="submit"
            loading={submitting}
            disabled={selectedCourseIds.length === 0 || loading}
          >
            Share {selectedCourseIds.length > 0 ? `${selectedCourseIds.length} ` : ""}
            Course{selectedCourseIds.length !== 1 ? "s" : ""}
          </Button>
        </div>
      </form>
    </Modal>
  );
}

export default CourseAssignmentModal;

