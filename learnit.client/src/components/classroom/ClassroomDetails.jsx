import { useState, useEffect } from "react";
import { useParams, useNavigate } from "react-router-dom";
import { classroomApi, courseApi } from "../../services";
import { Loading, ErrorMessage, ToastContainer, useToast } from "../ui/index";
import CourseAssignmentModal from "./CourseAssignmentModal";
import styles from "./ClassroomDetails.module.css";
import {
  FaArrowLeft,
  FaUsers,
  FaBook,
  FaCopy,
  FaShare,
  FaTrash,
  FaUserPlus,
  FaSignOutAlt,
  FaCheckCircle,
} from "react-icons/fa";
import { BsGraphUp } from "react-icons/bs";

function ClassroomDetails() {
  const { id } = useParams();
  const navigate = useNavigate();
  const toast = useToast();
  const [classroom, setClassroom] = useState(null);
  const [members, setMembers] = useState([]);
  const [sharedCourses, setSharedCourses] = useState([]);
  const [memberProgress, setMemberProgress] = useState([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState("");
  const [showShareModal, setShowShareModal] = useState(false);
  const [activeTab, setActiveTab] = useState("courses"); // "courses", "members", or "progress"

  useEffect(() => {
    if (id) {
      fetchClassroomData();
    }
  }, [id]);

  const fetchClassroomData = async () => {
    try {
      setLoading(true);
      setError("");
      
      if (!id) {
        setError("Classroom ID is missing");
        setLoading(false);
        return;
      }

      const [classroomData, membersData, coursesData, progressData] = await Promise.all([
        classroomApi.getClassroom(id),
        classroomApi.getMembers(id),
        classroomApi.getSharedCourses(id),
        classroomApi.getMemberProgress(id).catch((err) => {
          console.error("[ClassroomDetails] Failed to load progress:", err);
          return [];
        }), // Don't fail if progress fails
      ]);
      
      setClassroom(classroomData);
      setMembers(membersData || []);
      setSharedCourses(coursesData || []);
      setMemberProgress(progressData || []);
      
      // Debug logging
      console.log("[ClassroomDetails] Progress data loaded:", progressData);
      console.log("[ClassroomDetails] Member progress count:", progressData?.length || 0);
    } catch (err) {
      console.error("[ClassroomDetails] Error fetching data:", err);
      
      // Handle different error types
      let errorMessage = "Failed to load classroom";
      
      // Check for 404 status
      if (err.status === 404 || err.response?.status === 404) {
        errorMessage = "Classroom not found. You may not have access to this classroom or it may have been deleted.";
      } else if (err.data?.message) {
        errorMessage = err.data.message;
      } else if (typeof err.data === "string") {
        errorMessage = err.data;
      } else if (err.message) {
        errorMessage = err.message;
      }
      
      setError(errorMessage);
    } finally {
      setLoading(false);
    }
  };

  const handleShareCourses = async (courseIds) => {
    try {
      await classroomApi.shareCourses(id, courseIds);
      setShowShareModal(false);
      toast.success(`Successfully shared ${courseIds.length} course(s) to classroom!`);
      await fetchClassroomData();
    } catch (err) {
      toast.error(err.message || "Failed to share courses");
      throw err;
    }
  };

  const handleCopyCourse = async (classroomCourseId) => {
    try {
      const result = await classroomApi.copyCourse(classroomCourseId);
      toast.success(result.message || "Course copied to your library!");
      navigate(`/app/course/${result.copiedCourseId}`);
    } catch (err) {
      toast.error(err.message || "Failed to copy course");
    }
  };

  const handleUnshareCourse = async (courseId) => {
    if (!confirm("Are you sure you want to remove this course from the classroom?")) {
      return;
    }

    try {
      await classroomApi.unshareCourse(id, courseId);
      toast.success("Course removed from classroom");
      await fetchClassroomData();
    } catch (err) {
      toast.error(err.message || "Failed to remove course");
    }
  };

  const handleLeaveClassroom = async () => {
    if (!confirm("Are you sure you want to leave this classroom?")) {
      return;
    }

    try {
      await classroomApi.leaveClassroom(id);
      toast.success("Left classroom successfully");
      navigate("/app/classrooms");
    } catch (err) {
      toast.error(err.message || "Failed to leave classroom");
    }
  };

  const canManage = classroom && (classroom.isCreator || classroom.userRole === "Admin");

  if (loading) {
    return <Loading />;
  }

  if (error || !classroom) {
    return (
      <div className={styles.errorContainer}>
        <ErrorMessage message={error || "Classroom not found"} />
        <button 
          className={styles.backButton}
          onClick={() => navigate("/app/classrooms")}
        >
          <FaArrowLeft /> Back to Classrooms
        </button>
      </div>
    );
  }

  return (
    <div className={styles.classroomDetails}>
      <div className={styles.header}>
        <button className={styles.backButton} onClick={() => navigate("/app/classrooms")}>
          <FaArrowLeft /> Back to Classrooms
        </button>

        <div className={styles.headerInfo}>
          <div>
            <h1>{classroom.name}</h1>
            <p className={styles.description}>{classroom.description || "No description"}</p>
          </div>
          <div className={styles.headerActions}>
            {canManage && (
              <button
                className={styles.shareButton}
                onClick={() => setShowShareModal(true)}
              >
                <FaShare /> Share Courses
              </button>
            )}
            {!classroom.isCreator && (
              <button
                className={styles.leaveButton}
                onClick={handleLeaveClassroom}
              >
                <FaSignOutAlt /> Leave
              </button>
            )}
          </div>
        </div>

        <div className={styles.stats}>
          <div className={styles.stat}>
            <FaUsers />
            <div>
              <span className={styles.statValue}>{classroom.memberCount}</span>
              <span className={styles.statLabel}>Members</span>
            </div>
          </div>
          <div className={styles.stat}>
            <FaBook />
            <div>
              <span className={styles.statValue}>{classroom.courseCount}</span>
              <span className={styles.statLabel}>Courses</span>
            </div>
          </div>
          <div className={styles.stat}>
            <div className={styles.inviteCode}>
              <span className={styles.codeLabel}>Invite Code</span>
              <span className={styles.codeValue}>{classroom.inviteCode}</span>
            </div>
          </div>
        </div>
      </div>

      <div className={styles.tabs}>
        <button
          className={`${styles.tab} ${activeTab === "courses" ? styles.active : ""}`}
          onClick={() => setActiveTab("courses")}
        >
          <FaBook /> Courses ({sharedCourses.length})
        </button>
        <button
          className={`${styles.tab} ${activeTab === "members" ? styles.active : ""}`}
          onClick={() => setActiveTab("members")}
        >
          <FaUsers /> Members ({members.length})
        </button>
        <button
          className={`${styles.tab} ${activeTab === "progress" ? styles.active : ""}`}
          onClick={() => setActiveTab("progress")}
        >
          <BsGraphUp /> Progress
        </button>
      </div>

      <div className={styles.content}>
        {activeTab === "courses" && (
          <div className={styles.coursesSection}>
            {sharedCourses.length === 0 ? (
              <div className={styles.emptyState}>
                <FaBook className={styles.emptyIcon} />
                <h3>No Courses Shared Yet</h3>
                <p>
                  {canManage
                    ? "Share your first course to get started!"
                    : "No courses have been shared in this classroom yet."}
                </p>
                {canManage && (
                  <button
                    className={styles.shareButton}
                    onClick={() => setShowShareModal(true)}
                  >
                    <FaShare /> Share Your First Course
                  </button>
                )}
              </div>
            ) : (
              <div className={styles.coursesGrid}>
                {sharedCourses.map((course) => (
                  <div key={course.id} className={styles.courseCard}>
                    <div className={styles.courseHeader}>
                      <h3>{course.courseTitle}</h3>
                      {canManage && course.sharedByUserId === classroom.creatorId && (
                        <button
                          className={styles.unshareButton}
                          onClick={() => handleUnshareCourse(course.courseId)}
                          title="Remove from classroom"
                        >
                          <FaTrash />
                        </button>
                      )}
                    </div>
                    <p className={styles.courseDescription}>
                      {course.courseDescription || "No description"}
                    </p>
                    <div className={styles.courseMeta}>
                      <span className={styles.metaItem}>
                        {course.courseDifficulty || "N/A"}
                      </span>
                      <span className={styles.metaItem}>
                        {course.courseTotalEstimatedHours}h
                      </span>
                      <span className={styles.metaItem}>
                        {course.moduleCount} modules
                      </span>
                    </div>
                    <div className={styles.courseFooter}>
                      <span className={styles.sharedBy}>
                        Shared by {course.sharedByUserName}
                      </span>
                      {course.isCopied ? (
                        <span className={styles.copiedBadge}>Already Copied</span>
                      ) : (
                        <button
                          className={styles.copyButton}
                          onClick={() => handleCopyCourse(course.id)}
                        >
                          <FaCopy /> Copy to My Library
                        </button>
                      )}
                    </div>
                  </div>
                ))}
              </div>
            )}
          </div>
        )}
        {activeTab === "members" && (
          <div className={styles.membersSection}>
            <div className={styles.membersList}>
              {members.map((member) => (
                <div key={member.id} className={styles.memberCard}>
                  <div className={styles.memberInfo}>
                    <div className={styles.memberAvatar}>
                      {member.userName.charAt(0).toUpperCase()}
                    </div>
                    <div>
                      <div className={styles.memberName}>{member.userName}</div>
                      <div className={styles.memberEmail}>{member.userEmail}</div>
                    </div>
                  </div>
                  <div className={styles.memberMeta}>
                    <span className={`${styles.roleBadge} ${styles[member.role.toLowerCase()]}`}>
                      {member.role}
                    </span>
                    <span className={styles.joinedDate}>
                      Joined {new Date(member.joinedAt).toLocaleDateString()}
                    </span>
                  </div>
                </div>
              ))}
            </div>
          </div>
        )}
        {activeTab === "progress" && (
          <div className={styles.progressSection}>
            {memberProgress.length === 0 ? (
              <div className={styles.emptyState}>
                <BsGraphUp className={styles.emptyIcon} />
                <h3>No Progress Data Yet</h3>
                <p>Progress will appear here once members start copying and studying courses.</p>
                <p className={styles.hint}>
                  <strong>Note:</strong> Members need to copy courses from the classroom to their personal library first. 
                  Progress is tracked in their copied courses.
                </p>
              </div>
            ) : (
              <div className={styles.progressList}>
                {memberProgress.map((member) => (
                  <div key={member.userId} className={styles.progressCard}>
                    <div className={styles.progressHeader}>
                      <div className={styles.memberInfo}>
                        <div className={styles.memberAvatar}>
                          {member.userName.charAt(0).toUpperCase()}
                        </div>
                        <div>
                          <div className={styles.memberName}>{member.userName}</div>
                          <div className={styles.memberEmail}>{member.userEmail}</div>
                        </div>
                      </div>
                      <div className={styles.overallProgress}>
                        <div className={styles.progressBar}>
                          <div 
                            className={styles.progressFill}
                            style={{ width: `${member.progressPercentage}%` }}
                          />
                        </div>
                        <span className={styles.progressText}>
                          {member.progressPercentage.toFixed(1)}%
                        </span>
                      </div>
                    </div>
                    <div className={styles.progressStats}>
                      <div className={styles.stat}>
                        <span className={styles.statLabel}>Courses</span>
                        <span className={styles.statValue}>
                          {member.completedCourses} / {member.totalCourses}
                        </span>
                      </div>
                      <div className={styles.stat}>
                        <span className={styles.statLabel}>Modules</span>
                        <span className={styles.statValue}>
                          {member.completedModules} / {member.totalModules}
                        </span>
                      </div>
                      <div className={styles.stat}>
                        <span className={styles.statLabel}>Hours</span>
                        <span className={styles.statValue}>
                          {member.completedHours} / {member.totalHours}h
                        </span>
                      </div>
                    </div>
                    {member.courseProgress && member.courseProgress.length > 0 && (
                      <div className={styles.courseProgressList}>
                        <h4>Course Progress</h4>
                        {member.courseProgress.map((course) => (
                          <div key={course.courseId} className={styles.courseProgressItem}>
                            <div className={styles.courseProgressHeader}>
                              <span className={styles.courseTitle}>{course.courseTitle}</span>
                              {course.isCompleted && (
                                <FaCheckCircle className={styles.completedIcon} />
                              )}
                            </div>
                            <div className={styles.courseProgressBar}>
                              <div 
                                className={styles.courseProgressFill}
                                style={{ width: `${course.progressPercentage}%` }}
                              />
                            </div>
                            <div className={styles.courseProgressMeta}>
                              <span>{course.completedModules} / {course.totalModules} modules</span>
                              <span>{course.completedHours} / {course.totalHours}h</span>
                            </div>
                          </div>
                        ))}
                      </div>
                    )}
                  </div>
                ))}
              </div>
            )}
          </div>
        )}
      </div>

      <CourseAssignmentModal
        isOpen={showShareModal}
        onClose={() => setShowShareModal(false)}
        onSubmit={handleShareCourses}
      />

      <ToastContainer toasts={toast.toasts} removeToast={toast.removeToast} />
    </div>
  );
}

export default ClassroomDetails;

