// ---------- SHARED ----------
let token = localStorage.getItem('token');

if (!token && !window.location.pathname.includes('login.html') && !window.location.pathname.includes('index.html')) {
    window.location.href = 'login.html';
}

function escapeHtml(str) {
    if (!str) return '';
    return str.replace(/[&<>]/g, m => (m === '&' ? '&amp;' : m === '<' ? '&lt;' : '&gt;'));
}

// ---------- LOGIN PAGE ----------
if (window.location.pathname.includes('login.html') || window.location.pathname.includes('index.html')) {
    const loginBtn = document.getElementById('loginBtn');
    const showRegBtn = document.getElementById('showRegBtn');
    const regForm = document.getElementById('regForm');
    const registerBtn = document.getElementById('registerBtn');
    const loginMessage = document.getElementById('loginMessage');

    if (showRegBtn && regForm) {
        showRegBtn.addEventListener('click', () => {
            regForm.style.display = regForm.style.display === 'none' ? 'block' : 'none';
        });
    }

    if (loginBtn) {
        loginBtn.addEventListener('click', async () => {
            const email = document.getElementById('loginEmail').value;
            const password = document.getElementById('loginPassword').value;
            try {
                const res = await fetch('/api/Auth/login', {
                    method: 'POST',
                    headers: { 'Content-Type': 'application/json' },
                    body: JSON.stringify({ email, password })
                });
                if (res.ok) {
                    const data = await res.json();
                    localStorage.setItem('token', data.token);
                    window.location.href = 'dashboard.html';
                } else {
                    const errorText = await res.text();
                    loginMessage.innerText = errorText || 'Invalid credentials';
                    loginMessage.style.color = '#f99';
                }
            } catch (err) {
                loginMessage.innerText = 'Network error';
                loginMessage.style.color = '#f99';
            }
        });
    }

    if (registerBtn) {
        registerBtn.addEventListener('click', async () => {
            const email = document.getElementById('regEmail').value;
            const password = document.getElementById('regPassword').value;
            try {
                const res = await fetch('/api/Auth/register', {
                    method: 'POST',
                    headers: { 'Content-Type': 'application/json' },
                    body: JSON.stringify({ email, password })
                });
                if (res.ok) {
                    alert('Registration successful! Please login.');
                    regForm.style.display = 'none';
                } else {
                    alert('Registration failed');
                }
            } catch (err) {
                alert('Network error');
            }
        });
    }
}

// ---------- DASHBOARD PAGE ----------
if (window.location.pathname.includes('dashboard.html')) {
    const token = localStorage.getItem('token');
    const myRequestsDiv = document.getElementById('myRequestsList');
    const pendingDiv = document.getElementById('pendingList');

    async function loadDashboard() {
        try {
            // My requests
            const myRes = await fetch('/api/AccessRequest', {
                headers: { 'Authorization': `Bearer ${token}` }
            });
            const myRequests = await myRes.json();
            myRequestsDiv.innerHTML = myRequests.map(r => `
                <div class="request-card">
                    <strong>${escapeHtml(r.title)}</strong><br>
                    Resource: ${escapeHtml(r.resource)}<br>
                    Status: ${r.status}<br>
                    ${r.status === 'Draft' ? `<button class="submitBtn" data-id="${r.id}">Submit</button>` : ''}
                    <button class="viewBtn" data-id="${r.id}">View Details</button>
                </div>
            `).join('');

            document.querySelectorAll('.submitBtn').forEach(btn => {
                btn.addEventListener('click', async () => {
                    const id = btn.dataset.id;
                    const res = await fetch(`/api/AccessRequest/${id}/submit`, {
                        method: 'PATCH',
                        headers: { 'Authorization': `Bearer ${token}` }
                    });
                    if (res.ok) loadDashboard();
                    else alert('Submit failed');
                });
            });
            document.querySelectorAll('.viewBtn').forEach(btn => {
                btn.addEventListener('click', () => {
                    window.location.href = `request.html?id=${btn.dataset.id}`;
                });
            });

            // Pending approvals
            const pendingRes = await fetch('/api/AccessRequest/pending', {
                headers: { 'Authorization': `Bearer ${token}` }
            });
            if (pendingRes.ok) {
                const pending = await pendingRes.json();
                pendingDiv.innerHTML = pending.map(r => `
                    <div class="request-card">
                        <strong>${escapeHtml(r.title)}</strong><br>
                        Requester: ${r.createdBy}<br>
                        Resource: ${escapeHtml(r.resource)}<br>
                        <button class="approvePendingBtn" data-id="${r.id}">Approve</button>
                        <button class="rejectPendingBtn" data-id="${r.id}">Reject</button>
                        <button class="viewPendingBtn" data-id="${r.id}">View</button>
                    </div>
                `).join('');

                document.querySelectorAll('.approvePendingBtn').forEach(btn => {
                    btn.addEventListener('click', async () => {
                        const id = btn.dataset.id;
                        const res = await fetch(`/api/AccessRequest/${id}/approve`, {
                            method: 'POST',
                            headers: { 'Authorization': `Bearer ${token}`, 'Content-Type': 'application/json' },
                            body: JSON.stringify(null)
                        });
                        if (res.ok) loadDashboard();
                        else alert('Approve failed');
                    });
                });
                document.querySelectorAll('.rejectPendingBtn').forEach(btn => {
                    btn.addEventListener('click', async () => {
                        const id = btn.dataset.id;
                        const res = await fetch(`/api/AccessRequest/${id}/reject`, {
                            method: 'POST',
                            headers: { 'Authorization': `Bearer ${token}`, 'Content-Type': 'application/json' },
                            body: JSON.stringify(null)
                        });
                        if (res.ok) loadDashboard();
                        else alert('Reject failed');
                    });
                });
                document.querySelectorAll('.viewPendingBtn').forEach(btn => {
                    btn.addEventListener('click', () => {
                        window.location.href = `request.html?id=${btn.dataset.id}`;
                    });
                });
            }
        } catch (err) {
            console.error(err);
        }
    }

    document.getElementById('newRequestBtn')?.addEventListener('click', () => {
        window.location.href = 'create.html';
    });
    document.getElementById('logoutBtn')?.addEventListener('click', () => {
        localStorage.removeItem('token');
        window.location.href = 'login.html';
    });

    loadDashboard();
}

// ---------- CREATE REQUEST PAGE ----------
if (window.location.pathname.includes('create.html')) {
    const token = localStorage.getItem('token');
    const form = document.getElementById('requestForm');
    const cancelBtn = document.getElementById('cancelBtn');

    if (form) {
        form.addEventListener('submit', async (e) => {
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
                    alert('Request created!');
                    window.location.href = 'dashboard.html';
                } else {
                    alert('Creation failed');
                }
            } catch (err) {
                alert('Network error');
            }
        });
    }
    if (cancelBtn) {
        cancelBtn.addEventListener('click', () => window.location.href = 'dashboard.html');
    }
}

// ---------- REQUEST DETAIL PAGE ----------
if (window.location.pathname.includes('request.html')) {
    const token = localStorage.getItem('token');
    const urlParams = new URLSearchParams(window.location.search);
    const requestId = urlParams.get('id');
    const requestInfo = document.getElementById('requestInfo');
    const approvalSection = document.getElementById('approvalSection');

    async function loadRequest() {
        try {
            const res = await fetch(`/api/AccessRequest/${requestId}`, {
                headers: { 'Authorization': `Bearer ${token}` }
            });
            if (!res.ok) throw new Error('Not found');
            const req = await res.json();
            requestInfo.innerHTML = `
                <p><strong>Title:</strong> ${escapeHtml(req.title)}</p>
                <p><strong>Resource:</strong> ${escapeHtml(req.resource)}</p>
                <p><strong>Access Type:</strong> ${escapeHtml(req.accessType)}</p>
                <p><strong>Justification:</strong> ${escapeHtml(req.justification)}</p>
                <p><strong>Basis:</strong> ${escapeHtml(req.basis || '-')}</p>
                <p><strong>Status:</strong> ${req.status}</p>
                <p><strong>Created:</strong> ${new Date(req.createdAt).toLocaleString()}</p>
            `;
            if (req.status === 'Submitted') {
                approvalSection.innerHTML = `
                    <button id="approveBtn">Approve</button>
                    <button id="rejectBtn">Reject</button>
                `;
                document.getElementById('approveBtn').addEventListener('click', async () => {
                    const resp = await fetch(`/api/AccessRequest/${requestId}/approve`, {
                        method: 'POST',
                        headers: { 'Authorization': `Bearer ${token}`, 'Content-Type': 'application/json' },
                        body: JSON.stringify(null)
                    });
                    if (resp.ok) loadRequest();
                    else alert('Approve failed');
                });
                document.getElementById('rejectBtn').addEventListener('click', async () => {
                    const resp = await fetch(`/api/AccessRequest/${requestId}/reject`, {
                        method: 'POST',
                        headers: { 'Authorization': `Bearer ${token}`, 'Content-Type': 'application/json' },
                        body: JSON.stringify(null)
                    });
                    if (resp.ok) loadRequest();
                    else alert('Reject failed');
                });
            } else {
                approvalSection.innerHTML = '<p>No actions available.</p>';
            }
        } catch (err) {
            requestInfo.innerHTML = '<p>Request not found.</p>';
        }
    }

    document.getElementById('backBtn')?.addEventListener('click', () => {
        window.location.href = 'dashboard.html';
    });
    if (requestId) loadRequest();
}