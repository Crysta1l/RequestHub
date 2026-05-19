// Shared helpers
const API_BASE = ''; // relative to origin
let token = localStorage.getItem('token');

function handleFetchError(err) {
    console.error(err);
    showMessage('Network error. Is the API running?', true);
}

function showMessage(msg, isError = false) {
    const msgDiv = document.getElementById('message');
    if (msgDiv) {
        msgDiv.innerText = msg;
        msgDiv.style.color = isError ? '#f99' : '#9f9';
    }
}

// Redirect if not logged in (except on login page)
if (!token && !window.location.pathname.includes('login.html')) {
    window.location.href = 'login.html';
}

// ----- LOGIN PAGE -----
if (window.location.pathname.includes('login.html')) {
    const loginBtn = document.getElementById('loginBtn');
    if (loginBtn) {
        loginBtn.onclick = async () => {
            const email = document.getElementById('email').value;
            const password = document.getElementById('password').value;
            try {
                const res = await fetch('/api/Auth/login', {
                    method: 'POST',
                    headers: { 'Content-Type': 'application/json' },
                    body: JSON.stringify({ email, password })
                });
                const data = await res.json();
                if (res.ok) {
                    localStorage.setItem('token', data.token);
                    window.location.href = 'dashboard.html';
                } else {
                    showMessage('Login failed: ' + (data.title || data.message || 'Invalid credentials'), true);
                }
            } catch (err) {
                showMessage('Cannot connect to API', true);
            }
        };
    }
}

// ----- DASHBOARD PAGE -----
if (window.location.pathname.includes('dashboard.html')) {
    const token = localStorage.getItem('token');
    if (!token) window.location.href = 'login.html';

    async function loadDashboard() {
        try {
            // My requests
            const myRes = await fetch('/api/AccessRequest', {
                headers: { 'Authorization': `Bearer ${token}` }
            });
            const myRequests = await myRes.json();
            const listDiv = document.getElementById('requestsList');
            if (listDiv) {
                listDiv.innerHTML = myRequests.map(r => `
                    <div class="request-card" data-id="${r.id}">
                        <strong>${escapeHtml(r.title)}</strong><br>
                        Resource: ${escapeHtml(r.resource)}<br>
                        Status: ${r.status}<br>
                        <button class="viewBtn" data-id="${r.id}">View Details</button>
                    </div>
                `).join('');
                document.querySelectorAll('.viewBtn').forEach(btn => {
                    btn.onclick = () => window.location.href = `request.html?id=${btn.dataset.id}`;
                });
            }

            // Pending approvals
            const pendingRes = await fetch('/api/AccessRequest/pending', {
                headers: { 'Authorization': `Bearer ${token}` }
            });
            if (pendingRes.ok) {
                const pending = await pendingRes.json();
                const pendingDiv = document.getElementById('pendingList');
                if (pendingDiv) {
                    pendingDiv.innerHTML = pending.map(r => `
                        <div class="request-card" data-id="${r.id}">
                            <strong>${escapeHtml(r.title)}</strong> (Requester: ${r.createdBy})<br>
                            Resource: ${escapeHtml(r.resource)}<br>
                            <button class="approveBtn" data-id="${r.id}">Approve</button>
                            <button class="rejectBtn" data-id="${r.id}">Reject</button>
                        </div>
                    `).join('');
                    document.querySelectorAll('.approveBtn').forEach(btn => {
                        btn.onclick = async () => {
                            const id = btn.dataset.id;
                            const res = await fetch(`/api/AccessRequest/${id}/approve`, {
                                method: 'POST',
                                headers: {
                                    'Authorization': `Bearer ${token}`,
                                    'Content-Type': 'application/json'
                                },
                                body: JSON.stringify(null)
                            });
                            if (res.ok) loadDashboard();
                            else showMessage('Approve failed', true);
                        };
                    });
                    document.querySelectorAll('.rejectBtn').forEach(btn => {
                        btn.onclick = async () => {
                            const id = btn.dataset.id;
                            const res = await fetch(`/api/AccessRequest/${id}/reject`, {
                                method: 'POST',
                                headers: {
                                    'Authorization': `Bearer ${token}`,
                                    'Content-Type': 'application/json'
                                },
                                body: JSON.stringify(null)
                            });
                            if (res.ok) loadDashboard();
                            else showMessage('Reject failed', true);
                        };
                    });
                }
            }
        } catch (err) {
            handleFetchError(err);
        }
    }

    document.getElementById('newRequestBtn').onclick = () => window.location.href = 'create.html';
    document.getElementById('logoutBtn').onclick = () => {
        localStorage.removeItem('token');
        window.location.href = 'login.html';
    };
    loadDashboard();
}

// ----- CREATE REQUEST PAGE -----
if (window.location.pathname.includes('create.html')) {
    const form = document.getElementById('requestForm');
    const cancelBtn = document.getElementById('cancelBtn');
    const token = localStorage.getItem('token');

    form.onsubmit = async (e) => {
        e.preventDefault();
        const payload = {
            title: document.getElementById('title').value,
            resource: document.getElementById('resource').value,
            accessType: document.getElementById('accessType').value,
            justification: document.getElementById('justification').value,
            basis: document.getElementById('basis').value || null
        };
        try {
            const res = await fetch('/api/AccessRequest', {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json',
                    'Authorization': `Bearer ${token}`
                },
                body: JSON.stringify(payload)
            });
            if (res.ok) {
                showMessage('Request created! Redirecting...', false);
                setTimeout(() => window.location.href = 'dashboard.html', 1500);
            } else {
                const err = await res.text();
                showMessage('Error: ' + err, true);
            }
        } catch (err) {
            handleFetchError(err);
        }
    };
    if (cancelBtn) cancelBtn.onclick = () => window.location.href = 'dashboard.html';
}

// ----- REQUEST DETAIL PAGE -----
if (window.location.pathname.includes('request.html')) {
    const urlParams = new URLSearchParams(window.location.search);
    const requestId = urlParams.get('id');
    const token = localStorage.getItem('token');
    const infoDiv = document.getElementById('requestInfo');
    const approvalDiv = document.getElementById('approvalSection');

    async function loadRequest() {
        try {
            const res = await fetch(`/api/AccessRequest/${requestId}`, {
                headers: { 'Authorization': `Bearer ${token}` }
            });
            if (!res.ok) throw new Error('Not found');
            const req = await res.json();
            infoDiv.innerHTML = `
                <p><strong>Title:</strong> ${escapeHtml(req.title)}</p>
                <p><strong>Resource:</strong> ${escapeHtml(req.resource)}</p>
                <p><strong>Access Type:</strong> ${escapeHtml(req.accessType)}</p>
                <p><strong>Justification:</strong> ${escapeHtml(req.justification)}</p>
                <p><strong>Basis:</strong> ${escapeHtml(req.basis || '-')}</p>
                <p><strong>Status:</strong> ${req.status}</p>
                <p><strong>Created:</strong> ${new Date(req.createdAt).toLocaleString()}</p>
            `;
            // Show approve/reject if current user is approver and status is Submitted
            const role = localStorage.getItem('role'); // we could store role on login
            // For simplicity, just show buttons if status == "Submitted" (allow any logged user to act)
            // In real app, check role via /me endpoint.
            if (req.status === 'Submitted') {
                approvalDiv.innerHTML = `
                    <button id="approveBtn">Approve</button>
                    <button id="rejectBtn">Reject</button>
                `;
                document.getElementById('approveBtn').onclick = async () => {
                    const resp = await fetch(`/api/AccessRequest/${requestId}/approve`, {
                        method: 'POST',
                        headers: { 'Authorization': `Bearer ${token}`, 'Content-Type': 'application/json' },
                        body: JSON.stringify(null)
                    });
                    if (resp.ok) loadRequest();
                    else showMessage('Approve failed', true);
                };
                document.getElementById('rejectBtn').onclick = async () => {
                    const resp = await fetch(`/api/AccessRequest/${requestId}/reject`, {
                        method: 'POST',
                        headers: { 'Authorization': `Bearer ${token}`, 'Content-Type': 'application/json' },
                        body: JSON.stringify(null)
                    });
                    if (resp.ok) loadRequest();
                    else showMessage('Reject failed', true);
                };
            } else {
                approvalDiv.innerHTML = '<p>No actions available.</p>';
            }
        } catch (err) {
            infoDiv.innerHTML = '<p>Request not found.</p>';
        }
    }
    document.getElementById('backBtn').onclick = () => window.location.href = 'dashboard.html';
    if (requestId) loadRequest();
}

function escapeHtml(str) {
    if (!str) return '';
    return str.replace(/[&<>]/g, function(m) {
        if (m === '&') return '&amp;';
        if (m === '<') return '&lt;';
        if (m === '>') return '&gt;';
        return m;
    });
}

// reg
document.getElementById('showRegBtn')?.addEventListener('click', () => {
    document.getElementById('regForm').style.display = 'block';
});
document.getElementById('doRegBtn')?.addEventListener('click', async () => {
    const email = document.getElementById('regEmail').value;
    const password = document.getElementById('regPassword').value;
    const res = await fetch('/api/Auth/register', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ email, password })
    });
    if (res.ok) alert('Registered! You can now login.');
    else alert('Registration failed');
});