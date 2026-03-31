// --- app.js (Головне ядро TaskFlow) ---

const DB_KEY = 'taskflow_db';

// Ініціалізація бази даних
function initDB() {
    if (!localStorage.getItem(DB_KEY)) {
        localStorage.setItem(DB_KEY, JSON.stringify({
            users: [],
            projects: [],
            tasks: [],
            timeLogs: [],
            currentUser: null
        }));
    }
}
initDB();

function getDB() { return JSON.parse(localStorage.getItem(DB_KEY)); }
function saveDB(data) { localStorage.setItem(DB_KEY, JSON.stringify(data)); }

// ==========================================
// 1. АВТОРИЗАЦІЯ ТА РЕЄСТРАЦІЯ
// ==========================================
const Auth = {
    register: function(username, email, password) {
        const db = getDB();
        if (db.users.find(u => u.email === email)) return { success: false, msg: "Email вже існує" };
        
        const newUser = {
            id: 'user_' + Date.now(),
            username, email,
            password_hash: btoa(password) // Мок-хешування для фронтенду
        };
        db.users.push(newUser);
        saveDB(db);
        return { success: true };
    },
    login: function(email, password) {
        const db = getDB();
        const user = db.users.find(u => u.email === email && u.password_hash === btoa(password));
        if (user) {
            db.currentUser = { id: user.id, username: user.username, email: user.email };
            saveDB(db);
            return { success: true };
        }
        return { success: false, msg: "Невірний email або пароль" };
    },
    logout: function() {
        const db = getDB();
        db.currentUser = null;
        saveDB(db);
        window.location.replace('login.html');
    },
    getCurrentUser: function() {
        return getDB().currentUser;
    },
    requireAuth: function() {
        if (!this.getCurrentUser()) window.location.replace('login.html');
    }
};

// ==========================================
// 2. УПРАВЛІННЯ ПРОЄКТАМИ
// ==========================================
const Projects = {
    getAll: function() {
        const user = Auth.getCurrentUser();
        return getDB().projects.filter(p => p.user_id === user.id);
    },
    create: function(name) {
        const db = getDB();
        const newProject = { id: 'proj_' + Date.now(), user_id: db.currentUser.id, name };
        db.projects.push(newProject);
        saveDB(db);
        return newProject;
    },
    delete: function(id) {
        const db = getDB();
        db.projects = db.projects.filter(p => p.id !== id);
        db.tasks = db.tasks.filter(t => t.project_id !== id); // Видаляємо завдання проєкту
        saveDB(db);
    }
};

// ==========================================
// 3. ЗАВДАННЯ (З пріоритетами, дедлайнами та статусами)
// ==========================================
const Tasks = {
    getAll: function(projectId = null) {
        const user = Auth.getCurrentUser();
        let tasks = getDB().tasks.filter(t => t.user_id === user.id);
        if (projectId) tasks = tasks.filter(t => t.project_id === projectId);
        return tasks;
    },
    create: function(projectId, title, description, priority, dueDate) {
        const db = getDB();
        const newTask = {
            id: 'task_' + Date.now(),
            user_id: db.currentUser.id,
            project_id: projectId || null,
            title, description, priority, // priority: 'low', 'medium', 'high'
            status: 'todo', // status: 'todo', 'in_progress', 'done'
            due_date: dueDate,
            completed_at: null,
            created_at: new Date().toISOString()
        };
        db.tasks.push(newTask);
        saveDB(db);
        return newTask;
    },
    changeStatus: function(taskId, newStatus) {
        const db = getDB();
        const task = db.tasks.find(t => t.id === taskId);
        if (task) {
            task.status = newStatus;
            task.completed_at = newStatus === 'done' ? new Date().toISOString() : null;
            saveDB(db);
        }
    }
};

// ==========================================
// 4. ТАЙМ-ТРЕКІНГ (Логування часу)
// ==========================================
const TimeTracker = {
    addLog: function(taskId, timeSpentMinutes, comment) {
        const db = getDB();
        const log = {
            id: 'log_' + Date.now(),
            user_id: db.currentUser.id,
            task_id: taskId,
            time_spent: parseInt(timeSpentMinutes),
            comment: comment,
            created_at: new Date().toISOString()
        };
        db.timeLogs.push(log);
        saveDB(db);
    },
    getTaskTotalTime: function(taskId) {
        const logs = getDB().timeLogs.filter(l => l.task_id === taskId);
        return logs.reduce((total, log) => total + log.time_spent, 0); // Повертає загальний час у хвилинах
    }
};