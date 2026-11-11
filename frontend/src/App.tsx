import React, { useEffect, useState, type FormEvent } from 'react';
import {
  GraduationCap, LayoutDashboard, Users, ClipboardList, BookOpen,
  Megaphone, LogOut, Plus, KeyRound, Shield, Building2, Search,
  Lock, Unlock, Trash2, Clock, Send, FileText,
  RefreshCw, X
} from 'lucide-react';
import { api, token } from './api';

type User = {
  id: number;
  fullName: string;
  email: string;
  role: 'Admin' | 'Lecturer' | 'Student';
  status: 'Active' | 'Locked';
  phone?: string;
  createdAt?: string;
};

type Dept = {
  id: number;
  name: string;
  code: string;
  description?: string;
};

type ClassItem = {
  id: number;
  name: string;
  description?: string;
  joinCode?: string;
  status: string;
  createdAt?: string;
  department?: { id: number; name: string; code: string };
  lecturer?: { id: number; fullName: string; email: string };
  memberCount?: number;
  assignmentCount?: number;
};

type Member = {
  id: number;
  studentId: number;
  fullName: string;
  email: string;
  phone?: string;
  joinedAt: string;
  status: string;
};

type Assignment = {
  id: number;
  title: string;
  description: string;
  deadline: string;
  allowResubmit: boolean;
  maxAttempts: number;
  status: string;
  createdAt: string;
};

type Submission = {
  id: number;
  studentId: number;
  studentName?: string;
  studentEmail?: string;
  attemptNumber: number;
  content?: string;
  fileUrl?: string;
  submittedAt: string;
  score?: number;
  feedback?: string;
  gradedAt?: string;
};

type Announcement = {
  id: number;
  title: string;
  content: string;
  createdAt: string;
  author: { id: number; fullName: string; email: string };
};

const roleLabel = (r: string) => {
  if (r === 'Admin') return 'Quản trị viên';
  if (r === 'Lecturer') return 'Giảng viên';
  return 'Sinh viên';
};

export default function App() {
  // Auth state
  const [user, setUser] = useState<User | null>(null);
  const [authMode, setAuthMode] = useState<'login' | 'register'>('login');
  const [email, setEmail] = useState('admin@eduhub.local');
  const [password, setPassword] = useState('Admin@123');
  const [fullName, setFullName] = useState('');
  const [message, setMessage] = useState('');

  // Navigation
  const [page, setPage] = useState<'home' | 'classes' | 'users' | 'departments' | 'assignments' | 'announcements'>('home');

  // Data states
  const [classes, setClasses] = useState<ClassItem[]>([]);
  const [depts, setDepts] = useState<Dept[]>([]);
  const [selectedClass, setSelectedClass] = useState<ClassItem | null>(null);
  const [members, setMembers] = useState<Member[]>([]);
  const [assignments, setAssignments] = useState<Assignment[]>([]);
  const [submissions, setSubmissions] = useState<Submission[]>([]);
  const [selectedAssignment, setSelectedAssignment] = useState<Assignment | null>(null);
  const [announcements, setAnnouncements] = useState<Announcement[]>([]);

  // Admin users state
  const [userList, setUserList] = useState<User[]>([]);
  const [searchQuery, setSearchQuery] = useState('');
  const [filterRole, setFilterRole] = useState('');

  // Modals state
  const [showCreateClassModal, setShowCreateClassModal] = useState(false);
  const [showJoinModal, setShowJoinModal] = useState(false);
  const [showDeptModal, setShowDeptModal] = useState(false);
  const [showAssignModal, setShowAssignModal] = useState(false);
  const [showSubmitModal, setShowSubmitModal] = useState(false);
  const [showGradeModal, setShowGradeModal] = useState<Submission | null>(null);
  const [showAnnounceModal, setShowAnnounceModal] = useState(false);
  const [showPasswordModal, setShowPasswordModal] = useState(false);

  // Form states
  const [newClassName, setNewClassName] = useState('');
  const [newClassDesc, setNewClassDesc] = useState('');
  const [newClassDeptId, setNewClassDeptId] = useState<number>(1);
  const [joinCodeInput, setJoinCodeInput] = useState('');
  const [newDeptName, setNewDeptName] = useState('');
  const [newDeptCode, setNewDeptCode] = useState('');
  const [newDeptDesc, setNewDeptDesc] = useState('');
  const [assignTitle, setAssignTitle] = useState('');
  const [assignDesc, setAssignDesc] = useState('');
  const [assignDeadline, setAssignDeadline] = useState('');
  const [submitContent, setSubmitContent] = useState('');
  const [submitFileUrl, setSubmitFileUrl] = useState('');
  const [gradeScore, setGradeScore] = useState<number>(10);
  const [gradeFeedback, setGradeFeedback] = useState('');
  const [announceTitle, setAnnounceTitle] = useState('');
  const [announceContent, setAnnounceContent] = useState('');
  const [oldPassword, setOldPassword] = useState('');
  const [newPassword, setNewPassword] = useState('');

  async function loadInitial() {
    try {
      const u = await api('/api/auth/me');
      setUser(u);
      const [c, d] = await Promise.all([
        api('/api/classes/mine'),
        api('/api/departments')
      ]);
      setClasses(c || []);
      setDepts(d || []);
      if (d && d.length > 0) setNewClassDeptId(d[0].id);
    } catch {
      localStorage.removeItem('eduhub_token');
      setUser(null);
    }
  }

  useEffect(() => {
    if (token()) loadInitial();
  }, []);

  async function handleAuth(e: React.FormEvent) {
    e.preventDefault();
    setMessage('');
    try {
      if (authMode === 'login') {
        const res = await api('/api/auth/login', {
          method: 'POST',
          body: JSON.stringify({ email, password })
        });
        localStorage.setItem('eduhub_token', res.token);
        await loadInitial();
      } else {
        await api('/api/auth/register', {
          method: 'POST',
          body: JSON.stringify({ fullName, email, password })
        });
        setMessage('Đăng ký thành công! Đang tự động đăng nhập...');
        const res = await api('/api/auth/login', {
          method: 'POST',
          body: JSON.stringify({ email, password })
        });
        localStorage.setItem('eduhub_token', res.token);
        await loadInitial();
      }
    } catch (err) {
      setMessage((err as Error).message);
    }
  }

  function handleLogout() {
    localStorage.removeItem('eduhub_token');
    setUser(null);
    setPage('home');
    setSelectedClass(null);
  }

  // Load Admin Users
  async function loadUsers() {
    if (user?.role !== 'Admin') return;
    try {
      let path = `/api/users?pageSize=100`;
      if (searchQuery) path += `&q=${encodeURIComponent(searchQuery)}`;
      if (filterRole) path += `&role=${encodeURIComponent(filterRole)}`;
      const res = await api(path);
      setUserList(res.items || []);
    } catch (err) {
      setMessage((err as Error).message);
    }
  }

  useEffect(() => {
    if (page === 'users') loadUsers();
  }, [page, searchQuery, filterRole]);

  // Load Class Details
  async function selectClass(c: ClassItem) {
    setSelectedClass(c);
    try {
      const [m, a, ann] = await Promise.all([
        api(`/api/classes/${c.id}/members`),
        api(`/api/classes/${c.id}/assignments`),
        api(`/api/classes/${c.id}/announcements`)
      ]);
      setMembers(m || []);
      setAssignments(a || []);
      setAnnouncements(ann || []);
    } catch (err) {
      setMessage((err as Error).message);
    }
  }

  // Create Class
  async function handleCreateClass(e: React.FormEvent) {
    e.preventDefault();
    try {
      await api('/api/classes', {
        method: 'POST',
        body: JSON.stringify({
          name: newClassName,
          description: newClassDesc,
          departmentId: Number(newClassDeptId)
        })
      });
      setShowCreateClassModal(false);
      setNewClassName('');
      setNewClassDesc('');
      setMessage('Tạo lớp học thành công!');
      const c = await api('/api/classes/mine');
      setClasses(c || []);
    } catch (err) {
      setMessage((err as Error).message);
    }
  }

  // Join Class
  async function handleJoinClass(e: React.FormEvent) {
    e.preventDefault();
    try {
      await api('/api/classes/join', {
        method: 'POST',
        body: JSON.stringify({ joinCode: joinCodeInput })
      });
      setShowJoinModal(false);
      setJoinCodeInput('');
      setMessage('Tham gia lớp học thành công!');
      const c = await api('/api/classes/mine');
      setClasses(c || []);
    } catch (err) {
      setMessage((err as Error).message);
    }
  }

  // Regenerate Join Code
  async function handleRegenerateCode(classId: number) {
    if (!confirm('Bạn có chắc muốn cấp lại mã tham gia lớp học mới?')) return;
    try {
      const res = await api(`/api/classes/${classId}/regenerate-code`, { method: 'POST' });
      setMessage(`Đã tạo mã tham gia mới: ${res.joinCode}`);
      if (selectedClass && selectedClass.id === classId) {
        setSelectedClass({ ...selectedClass, joinCode: res.joinCode });
      }
      const c = await api('/api/classes/mine');
      setClasses(c || []);
    } catch (err) {
      setMessage((err as Error).message);
    }
  }

  // Remove Member
  async function handleRemoveMember(classId: number, studentId: number) {
    if (!confirm('Bạn có chắc muốn xóa sinh viên này khỏi lớp học?')) return;
    try {
      await api(`/api/classes/${classId}/members/${studentId}`, { method: 'DELETE' });
      setMessage('Đã xóa sinh viên khỏi lớp.');
      setMembers(members.filter(m => m.studentId !== studentId));
    } catch (err) {
      setMessage((err as Error).message);
    }
  }

  // Leave Class
  async function handleLeaveClass(classId: number) {
    if (!confirm('Bạn có chắc muốn rời khỏi lớp học này?')) return;
    try {
      await api(`/api/classes/${classId}/leave`, { method: 'DELETE' });
      setMessage('Đã rời khỏi lớp học.');
      setSelectedClass(null);
      const c = await api('/api/classes/mine');
      setClasses(c || []);
    } catch (err) {
      setMessage((err as Error).message);
    }
  }

  // Admin Change Role
  async function handleChangeRole(userId: number, newRole: string) {
    try {
      await api(`/api/users/${userId}/role`, {
        method: 'PUT',
        body: JSON.stringify({ role: newRole })
      });
      setMessage('Cập nhật vai trò thành công!');
      await loadUsers();
    } catch (err) {
      setMessage((err as Error).message);
    }
  }

  // Admin Toggle Status
  async function handleToggleStatus(userId: number, currentStatus: string) {
    const nextStatus = currentStatus === 'Active' ? 'Locked' : 'Active';
    try {
      await api(`/api/users/${userId}/status`, {
        method: 'PUT',
        body: JSON.stringify({ status: nextStatus })
      });
      setMessage(`Đã ${nextStatus === 'Active' ? 'mở khóa' : 'khóa'} tài khoản thành công!`);
      await loadUsers();
    } catch (err) {
      setMessage((err as Error).message);
    }
  }

  // Admin Create Department
  async function handleCreateDept(e: React.FormEvent) {
    e.preventDefault();
    try {
      await api('/api/departments', {
        method: 'POST',
        body: JSON.stringify({
          name: newDeptName,
          code: newDeptCode,
          description: newDeptDesc
        })
      });
      setShowDeptModal(false);
      setNewDeptName('');
      setNewDeptCode('');
      setNewDeptDesc('');
      setMessage('Thêm khoa mới thành công!');
      const d = await api('/api/departments');
      setDepts(d || []);
    } catch (err) {
      setMessage((err as Error).message);
    }
  }

  // Admin Delete Department
  async function handleDeleteDept(deptId: number) {
    if (!confirm('Bạn có chắc muốn xóa khoa này?')) return;
    try {
      await api(`/api/departments/${deptId}`, { method: 'DELETE' });
      setMessage('Đã xóa khoa.');
      const d = await api('/api/departments');
      setDepts(d || []);
    } catch (err) {
      setMessage((err as Error).message);
    }
  }

  // Create Assignment
  async function handleCreateAssignment(e: React.FormEvent) {
    e.preventDefault();
    if (!selectedClass) return;
    try {
      await api(`/api/classes/${selectedClass.id}/assignments`, {
        method: 'POST',
        body: JSON.stringify({
          title: assignTitle,
          description: assignDesc,
          deadline: new Date(assignDeadline).toISOString(),
          allowResubmit: true,
          maxAttempts: 2
        })
      });
      setShowAssignModal(false);
      setAssignTitle('');
      setAssignDesc('');
      setAssignDeadline('');
      setMessage('Tạo bài tập mới thành công!');
      const a = await api(`/api/classes/${selectedClass.id}/assignments`);
      setAssignments(a || []);
    } catch (err) {
      setMessage((err as Error).message);
    }
  }

  // Submit Assignment
  async function handleSubmitAssignment(e: React.FormEvent) {
    e.preventDefault();
    if (!selectedAssignment) return;
    try {
      await api(`/api/assignments/${selectedAssignment.id}/submissions`, {
        method: 'POST',
        body: JSON.stringify({
          content: submitContent,
          fileUrl: submitFileUrl
        })
      });
      setShowSubmitModal(false);
      setSubmitContent('');
      setSubmitFileUrl('');
      setMessage('Nộp bài tập thành công!');
      if (selectedClass) {
        const a = await api(`/api/classes/${selectedClass.id}/assignments`);
        setAssignments(a || []);
      }
    } catch (err) {
      setMessage((err as Error).message);
    }
  }

  // View Submissions (Lecturer)
  async function viewSubmissions(assign: Assignment) {
    setSelectedAssignment(assign);
    try {
      const res = await api(`/api/assignments/${assign.id}/submissions`);
      setSubmissions(res || []);
    } catch (err) {
      setMessage((err as Error).message);
    }
  }

  // Grade Submission
  async function handleGradeSubmission(e: React.FormEvent) {
    e.preventDefault();
    if (!showGradeModal) return;
    try {
      await api(`/api/submissions/${showGradeModal.id}/grade`, {
        method: 'PUT',
        body: JSON.stringify({
          score: Number(gradeScore),
          feedback: gradeFeedback
        })
      });
      setShowGradeModal(null);
      setMessage('Chấm điểm bài nộp thành công!');
      if (selectedAssignment) {
        const res = await api(`/api/assignments/${selectedAssignment.id}/submissions`);
        setSubmissions(res || []);
      }
    } catch (err) {
      setMessage((err as Error).message);
    }
  }

  // Create Announcement
  async function handleCreateAnnouncement(e: React.FormEvent) {
    e.preventDefault();
    if (!selectedClass) return;
    try {
      await api(`/api/classes/${selectedClass.id}/announcements`, {
        method: 'POST',
        body: JSON.stringify({
          title: announceTitle,
          content: announceContent
        })
      });
      setShowAnnounceModal(false);
      setAnnounceTitle('');
      setAnnounceContent('');
      setMessage('Đăng thông báo thành công!');
      const ann = await api(`/api/classes/${selectedClass.id}/announcements`);
      setAnnouncements(ann || []);
    } catch (err) {
      setMessage((err as Error).message);
    }
  }

  // Change Password
  async function handleChangePassword(e: React.FormEvent) {
    e.preventDefault();
    try {
      await api('/api/auth/change-password', {
        method: 'PUT',
        body: JSON.stringify({ oldPassword, newPassword })
      });
      setShowPasswordModal(false);
      setOldPassword('');
      setNewPassword('');
      setMessage('Đổi mật khẩu thành công!');
    } catch (err) {
      setMessage((err as Error).message);
    }
  }

  // -------------------------------------------------------------
  // LOGIN / REGISTER VIEW
  // -------------------------------------------------------------
  if (!user) {
    return (
      <div className="login-wrapper">
        <div className="login-card">
          <div className="login-brand">
            <GraduationCap size={36} />
            <h2>EduHub</h2>
          </div>
          <h3>{authMode === 'login' ? 'Đăng nhập vào hệ thống' : 'Đăng ký tài khoản sinh viên'}</h3>
          <p className="login-sub">Hệ thống quản lý lớp học và nộp bài trực tuyến</p>

          <form onSubmit={handleAuth} className="login-form">
            {authMode === 'register' && (
              <div className="form-group">
                <label>Họ và tên</label>
                <input
                  type="text"
                  placeholder="Nguyễn Văn A"
                  required
                  value={fullName}
                  onChange={e => setFullName(e.target.value)}
                />
              </div>
            )}

            <div className="form-group">
              <label>Địa chỉ Email</label>
              <input
                type="email"
                placeholder="name@eduhub.local"
                required
                value={email}
                onChange={e => setEmail(e.target.value)}
              />
            </div>

            <div className="form-group">
              <label>Mật khẩu</label>
              <input
                type="password"
                placeholder="••••••••"
                required
                value={password}
                onChange={e => setPassword(e.target.value)}
              />
            </div>

            <button type="submit" className="btn-primary">
              {authMode === 'login' ? 'Đăng nhập' : 'Tạo tài khoản'}
            </button>
          </form>

          {message && <div className="alert-box error">{message}</div>}

          <div className="auth-toggle">
            {authMode === 'login' ? (
              <p>Chưa có tài khoản? <span onClick={() => { setAuthMode('register'); setMessage(''); }}>Đăng ký ngay</span></p>
            ) : (
              <p>Đã có tài khoản? <span onClick={() => { setAuthMode('login'); setMessage(''); }}>Đăng nhập</span></p>
            )}
          </div>

          <div className="demo-accounts">
            <p>Chọn tài khoản Demo:</p>
            <div className="demo-btns">
              <button onClick={() => { setEmail('admin@eduhub.local'); setPassword('Admin@123'); }}>Admin</button>
              <button onClick={() => { setEmail('lecturer@eduhub.local'); setPassword('Lecturer@123'); }}>Giảng viên</button>
              <button onClick={() => { setEmail('student@eduhub.local'); setPassword('Student@123'); }}>Sinh viên</button>
            </div>
          </div>
        </div>
      </div>
    );
  }

  // -------------------------------------------------------------
  // MAIN DASHBOARD VIEW
  // -------------------------------------------------------------
  return (
    <div className="app-layout">
      {/* SIDEBAR NAVIGATION */}
      <aside className="sidebar">
        <div className="brand">
          <GraduationCap size={28} />
          <span>EduHub</span>
        </div>

        <div className="user-profile-badge" onClick={() => setShowPasswordModal(true)} title="Nhấn để đổi mật khẩu">
          <div className="avatar-circle">{user.fullName.charAt(0)}</div>
          <div className="user-meta">
            <b>{user.fullName}</b>
            <span className={`role-pill ${user.role.toLowerCase()}`}>{roleLabel(user.role)}</span>
          </div>
        </div>

        <nav className="nav-menu">
          <button className={page === 'home' ? 'active' : ''} onClick={() => { setPage('home'); setSelectedClass(null); }}>
            <LayoutDashboard size={18} /> Tổng quan
          </button>
          <button className={page === 'classes' ? 'active' : ''} onClick={() => setPage('classes')}>
            <Users size={18} /> Lớp học
          </button>
          <button className={page === 'assignments' ? 'active' : ''} onClick={() => setPage('assignments')}>
            <ClipboardList size={18} /> Bài tập
          </button>
          <button className={page === 'announcements' ? 'active' : ''} onClick={() => setPage('announcements')}>
            <Megaphone size={18} /> Thông báo
          </button>

          {/* ADMIN ONLY MENU */}
          {user.role === 'Admin' && (
            <>
              <div className="nav-divider">QUẢN TRỊ VIÊN</div>
              <button className={page === 'users' ? 'active' : ''} onClick={() => setPage('users')}>
                <Shield size={18} /> Người dùng
              </button>
              <button className={page === 'departments' ? 'active' : ''} onClick={() => setPage('departments')}>
                <Building2 size={18} /> Quản lý Khoa
              </button>
            </>
          )}
        </nav>

        <button className="logout-btn" onClick={handleLogout}>
          <LogOut size={18} /> Đăng xuất
        </button>
      </aside>

      {/* MAIN CONTENT AREA */}
      <main className="main-content">
        <header className="topbar">
          <div>
            <span className="page-category">HỆ THỐNG EDUHUB</span>
            <h1 className="page-title">
              {page === 'home' && 'Bảng điều khiển Tổng quan'}
              {page === 'classes' && (selectedClass ? selectedClass.name : 'Danh sách Lớp học')}
              {page === 'assignments' && 'Quản lý Bài tập & Nộp bài'}
              {page === 'announcements' && 'Thông báo lớp học'}
              {page === 'users' && 'Quản trị Danh sách Người dùng'}
              {page === 'departments' && 'Danh mục Khoa / Đơn vị đào tạo'}
            </h1>
          </div>

          <div className="topbar-user">
            <span>{user.email}</span>
            <button className="btn-sm" onClick={() => setShowPasswordModal(true)}>Đổi mật khẩu</button>
          </div>
        </header>

        {message && (
          <div className="notification-banner" onClick={() => setMessage('')}>
            <span>{message}</span>
            <X size={16} />
          </div>
        )}

        {/* ======================================================== */}
        {/* PAGE: HOME (TỔNG QUAN) */}
        {/* ======================================================== */}
        {page === 'home' && (
          <div className="page-body">
            <div className="welcome-banner">
              <div>
                <h2>Xin chào, {user.fullName}!</h2>
                <p>Hệ thống hỗ trợ quản lý lớp học, phân quyền, giao bài tập và thông báo trực tuyến.</p>
              </div>
              <GraduationCap size={64} className="banner-icon" />
            </div>

            <div className="stat-cards-grid">
              <div className="stat-card">
                <Users size={28} className="stat-icon blue" />
                <div>
                  <small>Lớp học của tôi</small>
                  <h3>{classes.length}</h3>
                </div>
              </div>

              <div className="stat-card">
                <Building2 size={28} className="stat-icon purple" />
                <div>
                  <small>Tổng số Khoa</small>
                  <h3>{depts.length}</h3>
                </div>
              </div>

              <div className="stat-card">
                <ClipboardList size={28} className="stat-icon green" />
                <div>
                  <small>Vai trò hiện tại</small>
                  <h3>{roleLabel(user.role)}</h3>
                </div>
              </div>
            </div>

            <div className="panel-box">
              <div className="panel-header">
                <h3>Lớp học gần đây</h3>
                <button className="btn-secondary" onClick={() => setPage('classes')}>Xem tất cả</button>
              </div>
              <ClassGrid items={classes.slice(0, 4)} onSelect={c => { setPage('classes'); selectClass(c); }} />
            </div>
          </div>
        )}

        {/* ======================================================== */}
        {/* PAGE: CLASSES (QUẢN LÝ LỚP HỌC) */}
        {/* ======================================================== */}
        {page === 'classes' && (
          <div className="page-body">
            {!selectedClass ? (
              <>
                <div className="action-toolbar">
                  <h3>Các lớp học trực thuộc ({classes.length})</h3>
                  <div className="toolbar-actions">
                    {user.role === 'Lecturer' && (
                      <button className="btn-primary" onClick={() => setShowCreateClassModal(true)}>
                        <Plus size={16} /> Tạo lớp học mới
                      </button>
                    )}
                    {user.role === 'Student' && (
                      <button className="btn-primary" onClick={() => setShowJoinModal(true)}>
                        <KeyRound size={16} /> Tham gia lớp bằng mã
                      </button>
                    )}
                  </div>
                </div>

                <ClassGrid items={classes} onSelect={selectClass} />
              </>
            ) : (
              <div className="class-detail-view">
                <button className="btn-back" onClick={() => setSelectedClass(null)}>
                  ← Quay lại danh sách lớp
                </button>

                <div className="class-header-card">
                  <div>
                    <span className="badge">{selectedClass.department?.name || 'Khoa đào tạo'}</span>
                    <h2>{selectedClass.name}</h2>
                    <p>{selectedClass.description || 'Không có mô tả cho lớp học này.'}</p>
                    {selectedClass.lecturer && (
                      <small className="lecturer-tag">Giảng viên: {selectedClass.lecturer.fullName} ({selectedClass.lecturer.email})</small>
                    )}
                  </div>

                  <div className="class-code-badge">
                    <small>MÃ THAM GIA LỚP</small>
                    <code>{selectedClass.joinCode || 'Ẩn với sinh viên'}</code>
                    {user.role === 'Lecturer' && (
                      <button className="btn-icon" title="Cấp lại mã mới" onClick={() => handleRegenerateCode(selectedClass.id)}>
                        <RefreshCw size={14} />
                      </button>
                    )}
                    {user.role === 'Student' && (
                      <button className="btn-danger-sm" onClick={() => handleLeaveClass(selectedClass.id)}>
                        Rời lớp
                      </button>
                    )}
                  </div>
                </div>

                <div className="members-section panel-box">
                  <div className="panel-header">
                    <h3>Danh sách sinh viên trong lớp ({members.length})</h3>
                  </div>

                  <table className="data-table">
                    <thead>
                      <tr>
                        <th>Họ và tên</th>
                        <th>Email</th>
                        <th>Số điện thoại</th>
                        <th>Ngày tham gia</th>
                        <th>Trạng thái</th>
                        {user.role === 'Lecturer' && <th>Thao tác</th>}
                      </tr>
                    </thead>
                    <tbody>
                      {members.map(m => (
                        <tr key={m.id}>
                          <td><b>{m.fullName}</b></td>
                          <td>{m.email}</td>
                          <td>{m.phone || 'Chưa cập nhật'}</td>
                          <td>{new Date(m.joinedAt).toLocaleDateString('vi-VN')}</td>
                          <td><span className="status-pill active">{m.status}</span></td>
                          {user.role === 'Lecturer' && (
                            <td>
                              <button className="btn-icon danger" onClick={() => handleRemoveMember(selectedClass.id, m.studentId)} title="Loại khỏi lớp">
                                <Trash2 size={16} />
                              </button>
                            </td>
                          )}
                        </tr>
                      ))}
                      {members.length === 0 && (
                        <tr><td colSpan={6} style={{ textAlign: 'center', padding: '20px' }}>Chưa có sinh viên tham gia lớp học này.</td></tr>
                      )}
                    </tbody>
                  </table>
                </div>
              </div>
            )}
          </div>
        )}

        {/* ======================================================== */}
        {/* PAGE: ADMIN USERS (FR-03 & FR-04) */}
        {/* ======================================================== */}
        {page === 'users' && user.role === 'Admin' && (
          <div className="page-body">
            <div className="filter-bar">
              <div className="search-box">
                <Search size={18} />
                <input
                  type="text"
                  placeholder="Tìm theo tên hoặc email..."
                  value={searchQuery}
                  onChange={e => setSearchQuery(e.target.value)}
                />
              </div>

              <select value={filterRole} onChange={e => setFilterRole(e.target.value)} className="role-filter">
                <option value="">Tất cả vai trò</option>
                <option value="Admin">Quản trị viên (Admin)</option>
                <option value="Lecturer">Giảng viên (Lecturer)</option>
                <option value="Student">Sinh viên (Student)</option>
              </select>
            </div>

            <div className="panel-box">
              <table className="data-table">
                <thead>
                  <tr>
                    <th>Họ và tên</th>
                    <th>Email</th>
                    <th>Vai trò (Phân quyền)</th>
                    <th>Trạng thái</th>
                    <th>Khóa / Mở khóa</th>
                  </tr>
                </thead>
                <tbody>
                  {userList.map(u => (
                    <tr key={u.id}>
                      <td><b>{u.fullName}</b></td>
                      <td>{u.email}</td>
                      <td>
                        <select
                          value={u.role}
                          onChange={e => handleChangeRole(u.id, e.target.value)}
                          className="role-select"
                        >
                          <option value="Admin">Admin</option>
                          <option value="Lecturer">Giảng viên</option>
                          <option value="Student">Sinh viên</option>
                        </select>
                      </td>
                      <td>
                        <span className={`status-pill ${u.status === 'Active' ? 'active' : 'locked'}`}>
                          {u.status === 'Active' ? 'Hoạt động' : 'Bị khóa'}
                        </span>
                      </td>
                      <td>
                        <button
                          className={`btn-status-toggle ${u.status === 'Active' ? 'lock' : 'unlock'}`}
                          onClick={() => handleToggleStatus(u.id, u.status)}
                        >
                          {u.status === 'Active' ? <><Lock size={14} /> Khóa</> : <><Unlock size={14} /> Mở khóa</>}
                        </button>
                      </td>
                    </tr>
                  ))}
                  {userList.length === 0 && (
                    <tr><td colSpan={5} style={{ textAlign: 'center', padding: '20px' }}>Không tìm thấy người dùng phù hợp.</td></tr>
                  )}
                </tbody>
              </table>
            </div>
          </div>
        )}

        {/* ======================================================== */}
        {/* PAGE: ADMIN DEPARTMENTS (FR-05 & FR-06) */}
        {/* ======================================================== */}
        {page === 'departments' && user.role === 'Admin' && (
          <div className="page-body">
            <div className="action-toolbar">
              <h3>Danh mục Khoa / Đơn vị ({depts.length})</h3>
              <button className="btn-primary" onClick={() => setShowDeptModal(true)}>
                <Plus size={16} /> Thêm khoa mới
              </button>
            </div>

            <div className="departments-grid">
              {depts.map(d => (
                <div className="dept-card" key={d.id}>
                  <div className="dept-header">
                    <span className="dept-code">{d.code}</span>
                    <button className="btn-icon danger" onClick={() => handleDeleteDept(d.id)} title="Xóa khoa">
                      <Trash2 size={16} />
                    </button>
                  </div>
                  <h4>{d.name}</h4>
                  <p>{d.description || 'Chưa có mô tả'}</p>
                </div>
              ))}
            </div>
          </div>
        )}

        {/* ======================================================== */}
        {/* PAGE: ASSIGNMENTS (BÀI TẬP & NỘP BÀI) */}
        {/* ======================================================== */}
        {page === 'assignments' && (
          <div className="page-body">
            {!selectedClass ? (
              <div className="select-class-guide">
                <BookOpen size={48} />
                <h3>Vui lòng chọn một lớp học để xem bài tập</h3>
                <ClassGrid items={classes} onSelect={selectClass} />
              </div>
            ) : (
              <div>
                <div className="action-toolbar">
                  <div>
                    <span className="badge">{selectedClass.name}</span>
                    <h3>Danh sách bài tập ({assignments.length})</h3>
                  </div>
                  {user.role === 'Lecturer' && (
                    <button className="btn-primary" onClick={() => setShowAssignModal(true)}>
                      <Plus size={16} /> Giao bài tập mới
                    </button>
                  )}
                </div>

                <div className="assignment-list">
                  {assignments.map(a => (
                    <div className="assignment-card" key={a.id}>
                      <div className="assign-info">
                        <h4>{a.title}</h4>
                        <p>{a.description}</p>
                        <small className="deadline"><Clock size={14} /> Hạn nộp: {new Date(a.deadline).toLocaleString('vi-VN')}</small>
                      </div>

                      <div className="assign-actions">
                        {user.role === 'Student' && (
                          <button className="btn-primary" onClick={() => { setSelectedAssignment(a); setShowSubmitModal(true); }}>
                            <Send size={15} /> Nộp bài
                          </button>
                        )}
                        {user.role === 'Lecturer' && (
                          <button className="btn-secondary" onClick={() => viewSubmissions(a)}>
                            <FileText size={15} /> Xem bài nộp ({submissions.length})
                          </button>
                        )}
                      </div>
                    </div>
                  ))}
                  {assignments.length === 0 && <p className="empty-hint">Chưa có bài tập nào được giao trong lớp này.</p>}
                </div>

                {/* SUBMISSIONS LIST (FOR LECTURER) */}
                {selectedAssignment && user.role === 'Lecturer' && (
                  <div className="submissions-panel panel-box">
                    <div className="panel-header">
                      <h3>Danh sách bài nộp: {selectedAssignment.title}</h3>
                    </div>

                    <table className="data-table">
                      <thead>
                        <tr>
                          <th>Sinh viên</th>
                          <th>Lần nộp</th>
                          <th>Thời gian nộp</th>
                          <th>Bài làm / Tệp đính kèm</th>
                          <th>Điểm số</th>
                          <th>Thao tác</th>
                        </tr>
                      </thead>
                      <tbody>
                        {submissions.map(s => (
                          <tr key={s.id}>
                            <td><b>{s.studentName}</b><br /><small>{s.studentEmail}</small></td>
                            <td>Lần #{s.attemptNumber}</td>
                            <td>{new Date(s.submittedAt).toLocaleString('vi-VN')}</td>
                            <td>
                              {s.content && <p className="sub-text">{s.content}</p>}
                              {s.fileUrl && <a href={s.fileUrl} target="_blank" rel="noreferrer" className="link-file">Xem tệp đính kèm</a>}
                            </td>
                            <td>
                              {s.score !== null && s.score !== undefined ? (
                                <span className="score-badge">{s.score}/100</span>
                              ) : (
                                <span className="score-badge ungraded">Chưa chấm</span>
                              )}
                            </td>
                            <td>
                              <button className="btn-sm" onClick={() => { setShowGradeModal(s); setGradeScore(s.score || 10); setGradeFeedback(s.feedback || ''); }}>
                                Chấm điểm
                              </button>
                            </td>
                          </tr>
                        ))}
                        {submissions.length === 0 && (
                          <tr><td colSpan={6} style={{ textAlign: 'center', padding: '20px' }}>Chưa có sinh viên nào nộp bài.</td></tr>
                        )}
                      </tbody>
                    </table>
                  </div>
                )}
              </div>
            )}
          </div>
        )}

        {/* ======================================================== */}
        {/* PAGE: ANNOUNCEMENTS (THÔNG BÁO) */}
        {/* ======================================================== */}
        {page === 'announcements' && (
          <div className="page-body">
            {!selectedClass ? (
              <div className="select-class-guide">
                <Megaphone size={48} />
                <h3>Vui lòng chọn lớp học để xem bảng tin thông báo</h3>
                <ClassGrid items={classes} onSelect={selectClass} />
              </div>
            ) : (
              <div>
                <div className="action-toolbar">
                  <div>
                    <span className="badge">{selectedClass.name}</span>
                    <h3>Bảng tin thông báo ({announcements.length})</h3>
                  </div>
                  {user.role === 'Lecturer' && (
                    <button className="btn-primary" onClick={() => setShowAnnounceModal(true)}>
                      <Plus size={16} /> Đăng thông báo mới
                    </button>
                  )}
                </div>

                <div className="announcement-stream">
                  {announcements.map(ann => (
                    <article className="announcement-card" key={ann.id}>
                      <div className="ann-header">
                        <b>{ann.title}</b>
                        <small>{new Date(ann.createdAt).toLocaleString('vi-VN')}</small>
                      </div>
                      <p className="ann-body">{ann.content}</p>
                      <div className="ann-author">Người đăng: {ann.author.fullName} ({ann.author.email})</div>
                    </article>
                  ))}
                  {announcements.length === 0 && <p className="empty-hint">Chưa có thông báo nào trong lớp này.</p>}
                </div>
              </div>
            )}
          </div>
        )}
      </main>

      {/* ======================================================== */}
      {/* MODALS */}
      {/* ======================================================== */}

      {/* Modal: Create Class */}
      {showCreateClassModal && (
        <div className="modal-overlay">
          <div className="modal-content">
            <div className="modal-header">
              <h3>Tạo lớp học mới</h3>
              <button className="btn-close" onClick={() => setShowCreateClassModal(false)}><X size={18} /></button>
            </div>
            <form onSubmit={handleCreateClass}>
              <div className="form-group">
                <label>Tên lớp học</label>
                <input required placeholder="vd: Lập trình Web nâng cao - Nhóm 02" value={newClassName} onChange={e => setNewClassName(e.target.value)} />
              </div>
              <div className="form-group">
                <label>Khoa trực thuộc</label>
                <select value={newClassDeptId} onChange={e => setNewClassDeptId(Number(e.target.value))}>
                  {depts.map(d => (
                    <option key={d.id} value={d.id}>{d.name} ({d.code})</option>
                  ))}
                </select>
              </div>
              <div className="form-group">
                <label>Mô tả lớp học</label>
                <textarea rows={3} placeholder="Mô tả tóm tắt nội dung môn học..." value={newClassDesc} onChange={e => setNewClassDesc(e.target.value)} />
              </div>
              <button type="submit" className="btn-primary full">Tạo lớp</button>
            </form>
          </div>
        </div>
      )}

      {/* Modal: Join Class */}
      {showJoinModal && (
        <div className="modal-overlay">
          <div className="modal-content">
            <div className="modal-header">
              <h3>Tham gia lớp học</h3>
              <button className="btn-close" onClick={() => setShowJoinModal(false)}><X size={18} /></button>
            </div>
            <form onSubmit={handleJoinClass}>
              <div className="form-group">
                <label>Mã tham gia lớp (8 ký tự do Giảng viên cấp)</label>
                <input required placeholder="vd: WEB01ADV" value={joinCodeInput} onChange={e => setJoinCodeInput(e.target.value.toUpperCase())} />
              </div>
              <button type="submit" className="btn-primary full">Tham gia</button>
            </form>
          </div>
        </div>
      )}

      {/* Modal: Create Department */}
      {showDeptModal && (
        <div className="modal-overlay">
          <div className="modal-content">
            <div className="modal-header">
              <h3>Thêm Khoa / Đơn vị mới</h3>
              <button className="btn-close" onClick={() => setShowDeptModal(false)}><X size={18} /></button>
            </div>
            <form onSubmit={handleCreateDept}>
              <div className="form-group">
                <label>Tên khoa</label>
                <input required placeholder="vd: An toàn Thông tin" value={newDeptName} onChange={e => setNewDeptName(e.target.value)} />
              </div>
              <div className="form-group">
                <label>Mã khoa</label>
                <input required placeholder="vd: ATTT" value={newDeptCode} onChange={e => setNewDeptCode(e.target.value.toUpperCase())} />
              </div>
              <div className="form-group">
                <label>Mô tả</label>
                <textarea rows={2} placeholder="Mô tả khoa..." value={newDeptDesc} onChange={e => setNewDeptDesc(e.target.value)} />
              </div>
              <button type="submit" className="btn-primary full">Thêm khoa</button>
            </form>
          </div>
        </div>
      )}

      {/* Modal: Create Assignment */}
      {showAssignModal && (
        <div className="modal-overlay">
          <div className="modal-content">
            <div className="modal-header">
              <h3>Giao bài tập mới</h3>
              <button className="btn-close" onClick={() => setShowAssignModal(false)}><X size={18} /></button>
            </div>
            <form onSubmit={handleCreateAssignment}>
              <div className="form-group">
                <label>Tiêu đề bài tập</label>
                <input required placeholder="vd: Bài tập lớn Giữa kỳ" value={assignTitle} onChange={e => setAssignTitle(e.target.value)} />
              </div>
              <div className="form-group">
                <label>Yêu cầu & Nội dung</label>
                <textarea rows={3} required placeholder="Mô tả yêu cầu..." value={assignDesc} onChange={e => setAssignDesc(e.target.value)} />
              </div>
              <div className="form-group">
                <label>Hạn nộp bài</label>
                <input type="datetime-local" required value={assignDeadline} onChange={e => setAssignDeadline(e.target.value)} />
              </div>
              <button type="submit" className="btn-primary full">Giao bài tập</button>
            </form>
          </div>
        </div>
      )}

      {/* Modal: Submit Assignment */}
      {showSubmitModal && selectedAssignment && (
        <div className="modal-overlay">
          <div className="modal-content">
            <div className="modal-header">
              <h3>Nộp bài: {selectedAssignment.title}</h3>
              <button className="btn-close" onClick={() => setShowSubmitModal(false)}><X size={18} /></button>
            </div>
            <form onSubmit={handleSubmitAssignment}>
              <div className="form-group">
                <label>Nội dung câu trả lời / Ghi chú</label>
                <textarea rows={3} placeholder="Mô tả bài làm..." value={submitContent} onChange={e => setSubmitContent(e.target.value)} />
              </div>
              <div className="form-group">
                <label>Link tệp bài nộp (URL)</label>
                <input type="url" placeholder="https://..." value={submitFileUrl} onChange={e => setSubmitFileUrl(e.target.value)} />
              </div>
              <button type="submit" className="btn-primary full">Xác nhận Nộp bài</button>
            </form>
          </div>
        </div>
      )}

      {/* Modal: Grade Submission */}
      {showGradeModal && (
        <div className="modal-overlay">
          <div className="modal-content">
            <div className="modal-header">
              <h3>Chấm điểm bài nộp</h3>
              <button className="btn-close" onClick={() => setShowGradeModal(null)}><X size={18} /></button>
            </div>
            <form onSubmit={handleGradeSubmission}>
              <div className="form-group">
                <label>Điểm số (Thang điểm 0 - 100)</label>
                <input type="number" min="0" max="100" step="0.5" required value={gradeScore} onChange={e => setGradeScore(Number(e.target.value))} />
              </div>
              <div className="form-group">
                <label>Nhận xét / Đánh giá</label>
                <textarea rows={3} placeholder="Nhận xét cho sinh viên..." value={gradeFeedback} onChange={e => setGradeFeedback(e.target.value)} />
              </div>
              <button type="submit" className="btn-primary full">Lưu điểm</button>
            </form>
          </div>
        </div>
      )}

      {/* Modal: Create Announcement */}
      {showAnnounceModal && (
        <div className="modal-overlay">
          <div className="modal-content">
            <div className="modal-header">
              <h3>Đăng thông báo lớp</h3>
              <button className="btn-close" onClick={() => setShowAnnounceModal(false)}><X size={18} /></button>
            </div>
            <form onSubmit={handleCreateAnnouncement}>
              <div className="form-group">
                <label>Tiêu đề</label>
                <input required placeholder="vd: Lịch kiểm tra bù" value={announceTitle} onChange={e => setAnnounceTitle(e.target.value)} />
              </div>
              <div className="form-group">
                <label>Nội dung</label>
                <textarea rows={4} required placeholder="Nội dung thông báo..." value={announceContent} onChange={e => setAnnounceContent(e.target.value)} />
              </div>
              <button type="submit" className="btn-primary full">Đăng thông báo</button>
            </form>
          </div>
        </div>
      )}

      {/* Modal: Change Password */}
      {showPasswordModal && (
        <div className="modal-overlay">
          <div className="modal-content">
            <div className="modal-header">
              <h3>Đổi mật khẩu tài khoản</h3>
              <button className="btn-close" onClick={() => setShowPasswordModal(false)}><X size={18} /></button>
            </div>
            <form onSubmit={handleChangePassword}>
              <div className="form-group">
                <label>Mật khẩu hiện tại</label>
                <input type="password" required value={oldPassword} onChange={e => setOldPassword(e.target.value)} />
              </div>
              <div className="form-group">
                <label>Mật khẩu mới (Tối thiểu 8 ký tự)</label>
                <input type="password" required minLength={8} value={newPassword} onChange={e => setNewPassword(e.target.value)} />
              </div>
              <button type="submit" className="btn-primary full">Xác nhận đổi</button>
            </form>
          </div>
        </div>
      )}
    </div>
  );
}

// -------------------------------------------------------------
// REUSABLE CLASS GRID COMPONENT
// -------------------------------------------------------------
function ClassGrid({ items, onSelect }: { items: ClassItem[]; onSelect: (c: ClassItem) => void }) {
  if (items.length === 0) {
    return <p className="empty-hint">Chưa có lớp học nào trong danh sách.</p>;
  }

  return (
    <div className="classgrid">
      {items.map(c => (
        <div className="classcard" key={c.id} onClick={() => onSelect(c)}>
          <div className="classicon">
            <GraduationCap size={22} />
          </div>
          <b>{c.name}</b>
          <small>{c.department?.name || 'Khoa đào tạo'}</small>
          {c.joinCode && <span className="join-code-tag">Mã lớp: <code>{c.joinCode}</code></span>}
        </div>
      ))}
    </div>
  );
}
