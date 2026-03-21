import { useEffect, useState } from "react";
import { useNavigate } from "react-router-dom";
import { courseApi } from "../../services";
import CourseList from "../course/CourseList";
import CreateCourseModal from "../course/CreateCourseModal";
import EditCourseModal from "../course/EditCourseModal";
import { Loading, ErrorMessage, ToastContainer, useToast } from "../ui/index";
import styles from "./Course.module.css";

function Course() {
  const navigate = useNavigate();
  const toast = useToast();
  const [courses, setCourses] = useState([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState("");
  const [actionLoading, setActionLoading] = useState(false);

  const [showCreate, setShowCreate] = useState(false);
  const [createModalKey, setCreateModalKey] = useState(0);
  const [showEdit, setShowEdit] = useState(false);
  const [editingCourse, setEditingCourse] = useState(null);

  useEffect(() => {
    fetchCourses();
  }, []);

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

  const handleCreateCourse = async (formData) => {
    try {
      setActionLoading(true);
      setError("");
      await courseApi.createCourse(formData);
      setShowCreate(false);
      toast.success("Course created successfully!");
      await fetchCourses();
    } catch (err) {
      const errorMsg = err.message || "Failed to create course";
      setError(errorMsg);
      toast.error(errorMsg);
    } finally {
      setActionLoading(false);
    }
  };

  const handleEditCourse = async (courseId) => {
    try {
      setActionLoading(true);
      setError("");
      const course = await courseApi.getCourse(courseId);
      setEditingCourse(course);
      setShowEdit(true);
    } catch (err) {
      setError(err.message || "Failed to load course for editing");
    } finally {
      setActionLoading(false);
    }
  };

  const handleUpdateCourse = async (formData) => {
    try {
      setActionLoading(true);
      setError("");
      // Use editCourse endpoint which handles full course updates including modules
      await courseApi.editCourse(editingCourse.id, formData);
      setShowEdit(false);
      setEditingCourse(null);
      toast.success("Course updated successfully!");
      await fetchCourses();
    } catch (err) {
      const errorMsg = err.message || "Failed to update course";
      setError(errorMsg);
      toast.error(errorMsg);
    } finally {
      setActionLoading(false);
    }
  };

  const handleDeleteCourse = async (id) => {
    if (!confirm("Are you sure you want to delete this course?")) return;

    try {
      setActionLoading(true);
      setError("");
      await courseApi.deleteCourse(id);
      toast.success("Course deleted successfully");
      await fetchCourses();
    } catch (err) {
      const errorMsg = err.message || "Failed to delete course";
      setError(errorMsg);
      toast.error(errorMsg);
    } finally {
      setActionLoading(false);
    }
  };

  return (
    <section className={styles.page}>
      <ToastContainer toasts={toast.toasts} removeToast={toast.removeToast} />
      <ErrorMessage
        error={error}
        onRetry={fetchCourses}
        onDismiss={() => setError("")}
        title="Error loading courses"
      />

      {loading ? (
        <Loading message="Loading courses..." />
      ) : (
        <CourseList
          courses={courses}
          loading={false}
          onNavigate={(id) => navigate(`/app/course/${id}`)}
          onEdit={handleEditCourse}
          onDelete={handleDeleteCourse}
          onCreate={() => {
            setCreateModalKey(prev => prev + 1);
            setShowCreate(true);
          }}
        />
      )}

      {showCreate && (
        <CreateCourseModal
          key={`create-modal-${createModalKey}`}
          onSave={handleCreateCourse}
          onCancel={() => {
            setShowCreate(false);
          }}
          loading={actionLoading}
        />
      )}
      {showEdit && editingCourse && (
        <EditCourseModal
          course={editingCourse}
          onSave={handleUpdateCourse}
          onCancel={() => {
            setShowEdit(false);
            setEditingCourse(null);
          }}
          loading={actionLoading}
        />
      )}
    </section>
  );
}

export default Course;
