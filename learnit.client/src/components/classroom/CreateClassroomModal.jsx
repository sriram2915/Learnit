import { useState, useEffect } from "react";
import Modal from "../ui/Modal";
import Button from "../ui/Button";
import Field from "../ui/Field";
import { courseApi } from "../../services";
import { Loading } from "../ui/index";
import styles from "./CreateClassroomModal.module.css";
import { FaCheck, FaBook } from "react-icons/fa";

function CreateClassroomModal({ isOpen, onClose, onSubmit }) {
  const [formData, setFormData] = useState({
    name: "",
    description: "",
    isPublic: false,
  });
  const [courses, setCourses] = useState([]);
  const [selectedCourseIds, setSelectedCourseIds] = useState([]);
  const [showCourseSelection, setShowCourseSelection] = useState(false);
  const [loadingCourses, setLoadingCourses] = useState(false);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState("");

  useEffect(() => {
    if (isOpen && showCourseSelection) {
      fetchCourses();
    }
  }, [isOpen, showCourseSelection]);

  const fetchCourses = async () => {
    try {
      setLoadingCourses(true);
      const data = await courseApi.getCourses();
      setCourses(data || []);
    } catch (err) {
      console.error("Failed to load courses:", err);
    } finally {
      setLoadingCourses(false);
    }
  };

  const handleChange = (e) => {
    const { name, value, type, checked } = e.target;
    setFormData((prev) => ({
      ...prev,
      [name]: type === "checkbox" ? checked : value,
    }));
  };

  const handleToggleCourse = (courseId) => {
    setSelectedCourseIds((prev) =>
      prev.includes(courseId)
        ? prev.filter((id) => id !== courseId)
        : [...prev, courseId]
    );
  };

  const handleSubmit = async (e) => {
    e.preventDefault();
    setError("");

    if (!formData.name.trim()) {
      setError("Classroom name is required");
      return;
    }

    try {
      setLoading(true);
      // First create the classroom
      const classroom = await onSubmit(formData);
      
      // If courses are selected, share them to the classroom
      if (selectedCourseIds.length > 0 && classroom?.id) {
        try {
          const { classroomApi } = await import("../../services");
          await classroomApi.shareCourses(classroom.id, selectedCourseIds);
        } catch (shareErr) {
          console.error("Failed to share courses:", shareErr);
          // Don't fail the whole creation if course sharing fails
        }
      }
      
      // Reset form
      setFormData({ name: "", description: "", isPublic: false });
      setSelectedCourseIds([]);
      setShowCourseSelection(false);
    } catch (err) {
      // Error handled by parent
    } finally {
      setLoading(false);
    }
  };

  if (!isOpen) return null;

  return (
    <Modal isOpen={isOpen} onClose={onClose} title="Create New Classroom">
      <form onSubmit={handleSubmit} className={styles.form}>
        {error && <div className={styles.error}>{error}</div>}

        <Field label="Classroom Name" required>
          <input
            type="text"
            name="name"
            value={formData.name}
            onChange={handleChange}
            placeholder="e.g., Web Development Bootcamp"
            required
            className={styles.input}
          />
        </Field>

        <Field label="Description">
          <textarea
            name="description"
            value={formData.description}
            onChange={handleChange}
            placeholder="Describe what this classroom is about..."
            rows={4}
            className={styles.textarea}
          />
        </Field>

        <div className={styles.checkboxField}>
          <label className={styles.checkboxLabel}>
            <input
              type="checkbox"
              name="isPublic"
              checked={formData.isPublic}
              onChange={handleChange}
              className={styles.checkbox}
            />
            <span>Make this classroom public (discoverable by others)</span>
          </label>
        </div>

        <div className={styles.courseSelectionSection}>
          <div className={styles.courseSelectionHeader}>
            <label className={styles.checkboxLabel}>
              <input
                type="checkbox"
                checked={showCourseSelection}
                onChange={(e) => setShowCourseSelection(e.target.checked)}
                className={styles.checkbox}
              />
              <span>Add courses now (optional)</span>
            </label>
            {showCourseSelection && selectedCourseIds.length > 0 && (
              <span className={styles.selectedCount}>
                {selectedCourseIds.length} course{selectedCourseIds.length !== 1 ? "s" : ""} selected
              </span>
            )}
          </div>

          {showCourseSelection && (
            <div className={styles.coursesList}>
              {loadingCourses ? (
                <Loading />
              ) : courses.length === 0 ? (
                <div className={styles.emptyCourses}>
                  <FaBook className={styles.emptyIcon} />
                  <p>You don't have any courses yet.</p>
                  <p className={styles.hint}>You can add courses to this classroom later.</p>
                </div>
              ) : (
                courses.map((course) => {
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
                      </div>
                    </div>
                  );
                })
              )}
            </div>
          )}
        </div>

        <div className={styles.actions}>
          <Button type="button" variant="secondary" onClick={onClose}>
            Cancel
          </Button>
          <Button type="submit" loading={loading}>
            Create Classroom{selectedCourseIds.length > 0 ? ` (${selectedCourseIds.length} course${selectedCourseIds.length !== 1 ? "s" : ""})` : ""}
          </Button>
        </div>
      </form>
    </Modal>
  );
}

export default CreateClassroomModal;

