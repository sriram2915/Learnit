import { useState, useEffect } from "react";
import Modal from "../ui/Modal";
import Button from "../ui/Button";
import { classroomApi } from "../../services";
import { Loading } from "../ui/index";
import styles from "./ShareToClassroomModal.module.css";
import { FaUsers, FaCheck } from "react-icons/fa";

function ShareToClassroomModal({ isOpen, onClose, courseId, onSubmit }) {
  const [classrooms, setClassrooms] = useState([]);
  const [selectedClassroomIds, setSelectedClassroomIds] = useState([]);
  const [loading, setLoading] = useState(true);
  const [submitting, setSubmitting] = useState(false);
  const [error, setError] = useState("");

  useEffect(() => {
    if (isOpen) {
      fetchClassrooms();
    } else {
      setSelectedClassroomIds([]);
      setError("");
    }
  }, [isOpen]);

  const fetchClassrooms = async () => {
    try {
      setLoading(true);
      setError("");
      const data = await classroomApi.getClassrooms();
      // Filter to only show classrooms where user can share (Creator or Admin)
      const manageableClassrooms = data.filter(
        (c) => c.isCreator || c.userRole === "Admin"
      );
      setClassrooms(manageableClassrooms);
    } catch (err) {
      setError(err.message || "Failed to load classrooms");
    } finally {
      setLoading(false);
    }
  };

  const handleToggleClassroom = (classroomId) => {
    setSelectedClassroomIds((prev) =>
      prev.includes(classroomId)
        ? prev.filter((id) => id !== classroomId)
        : [...prev, classroomId]
    );
  };

  const handleSubmit = async (e) => {
    e.preventDefault();
    setError("");

    if (selectedClassroomIds.length === 0) {
      setError("Please select at least one classroom");
      return;
    }

    try {
      setSubmitting(true);
      // Share course to all selected classrooms
      await Promise.all(
        selectedClassroomIds.map((classroomId) =>
          classroomApi.shareCourses(classroomId, [courseId])
        )
      );
      await onSubmit(selectedClassroomIds.length);
      setSelectedClassroomIds([]);
    } catch (err) {
      setError(err.message || "Failed to share course");
    } finally {
      setSubmitting(false);
    }
  };

  if (!isOpen) return null;

  return (
    <Modal isOpen={isOpen} onClose={onClose} title="Share Course to Classrooms">
      <form onSubmit={handleSubmit} className={styles.form}>
        <div className={styles.info}>
          <p>Select one or more classrooms to share this course with.</p>
          <p className={styles.hint}>
            Only classrooms where you are a Creator or Admin are shown.
          </p>
        </div>

        {error && <div className={styles.error}>{error}</div>}

        {loading ? (
          <Loading />
        ) : classrooms.length === 0 ? (
          <div className={styles.emptyState}>
            <FaUsers className={styles.emptyIcon} />
            <p>You don't have any classrooms yet.</p>
            <p>Create a classroom first to share courses.</p>
            <Button
              variant="primary"
              onClick={() => {
                onClose();
                // Navigate to classrooms page - parent should handle this
                window.location.href = "/app/classrooms";
              }}
            >
              Go to Classrooms
            </Button>
          </div>
        ) : (
          <>
            <div className={styles.classroomsList}>
              {classrooms.map((classroom) => {
                const isSelected = selectedClassroomIds.includes(classroom.id);
                return (
                  <div
                    key={classroom.id}
                    className={`${styles.classroomItem} ${isSelected ? styles.selected : ""}`}
                    onClick={() => handleToggleClassroom(classroom.id)}
                  >
                    <div className={styles.checkbox}>
                      {isSelected && <FaCheck />}
                    </div>
                    <div className={styles.classroomInfo}>
                      <h4>{classroom.name}</h4>
                      <p className={styles.classroomDescription}>
                        {classroom.description || "No description"}
                      </p>
                      <div className={styles.classroomMeta}>
                        <span>
                          <FaUsers /> {classroom.memberCount} members
                        </span>
                        <span>•</span>
                        <span>{classroom.courseCount} courses</span>
                        {classroom.isCreator && (
                          <>
                            <span>•</span>
                            <span className={styles.creatorBadge}>Creator</span>
                          </>
                        )}
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
            disabled={selectedClassroomIds.length === 0 || loading}
          >
            Share to {selectedClassroomIds.length > 0 ? `${selectedClassroomIds.length} ` : ""}
            Classroom{selectedClassroomIds.length !== 1 ? "s" : ""}
          </Button>
        </div>
      </form>
    </Modal>
  );
}

export default ShareToClassroomModal;

