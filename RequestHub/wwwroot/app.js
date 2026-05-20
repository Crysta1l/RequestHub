// =====================
// SHARED UTILITIES
// =====================

const token = localStorage.getItem('token');

// Redirect to login if not authenticated
if (!token && !window.location.pathname.includes('login.html') && !window.location.pathname.includes('index.html')) {
    window.location.href = 'login.html';
}

// Escape HTML to prevent XSS
function escapeHtml(str) {
    if (!str) return '';
    return str.replace(/[&<>]/g, m => (m === '&' ? '&amp;' : m === '<' ? '&lt;' : '&gt;'));
}

// Decode JWT and get user role
function getUserRole() {
    if (!token) return '';
    try {
        const payload = JSON.parse(atob(token.split('.')[1]));
        return payload['http://schemas.microsoft.com/ws/2008/06/identity/claims/role'] || '';
    } catch {
        return '';
    }
}

// Return a badge HTML string based on status
function statusBadge(status) {
    const map = {
        'Draft':      'badge-draft',
        'Submitted':  'badge-submitted',
        'InApproval': 'badge-inapproval',
        'Approved':   'badge-approved',
        'Rejected':   'badge-rejected',
    };
    const cls = map[status] || 'badge-draft';
    return `<span class="badge ${cls}">${escapeHtml(status)}</span>`;
}

// =====================
// LOGIN PAGE
// =====================
if (window.location.pathname.includes('login.html') || window.location.pathname.includes('index.html') || window.location.pathname === '/') {

    const msg = document.getElementById('authMessage');

    function getCredentials() {
        return {
            email:    document.getElementById('email').value,
            password: document.getElementById('password').value
        };
    }

    // Login button
    document.getElementById('loginBtn').addEventListener('click', async () => {
        const { email, password } = getCredentials();
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
                msg.innerText = 'Invalid email or password';
                msg.className = 'message message-error';
            }
        } catch {
            msg.innerText = 'Network error';
            msg.className = 'message message-error';
        }
    });

    // Register button — same fields, different endpoint
    document.getElementById('registerBtn').addEventListener('click', async () => {
        const { email, password } = getCredentials();
        try {
            const res = await fetch('/api/Auth/register', {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify({ email, password })
            });
            if (res.ok) {
                msg.innerText = 'Account created! You can now login.';
                msg.className = 'message message-success';
            } else {
                msg.innerText = 'Registration failed. Email may already exist.';
                msg.className = 'message message-error';
            }
        } catch {
            msg.innerText = 'Network error';
            msg.className = 'message message-error';
        }
    });
}

// =====================
// DASHBOARD PAGE
// =====================
if (window.location.pathname.includes('dashboard.html')) {

    const myRequestsDiv = document.getElementById('myRequestsList');
    const pendingDiv    = document.getElementById('pendingList');
    const role          = getUserRole();

    async function loadDashboard() {
        try {
            // Load my requests
            const myRes = await fetch('/api/AccessRequest', {
                headers: { 'Authorization': `Bearer ${token}` }
            });
            const myRequests = await myRes.json();

            if (myRequests.length === 0) {
                myRequestsDiv.innerHTML = '<div class="empty-state">No requests yet. Create your first one!</div>';
            } else {
                myRequestsDiv.innerHTML = myRequests.map(r => `
                    <div class="request-card">
                        <div class="request-card-title">${escapeHtml(r.title)}</div>
                        <div class="request-card-meta">Resource: ${escapeHtml(r.resource)}</div>
                        <div class="request-card-meta">Access: ${escapeHtml(r.accessType)}</div>
                        <div class="request-card-meta">Status: ${statusBadge(r.status)}</div>
                        <div class="request-card-actions">
                            ${r.status === 'Draft' ? `<button class="btn-sm submitBtn" data-id="${r.id}">Submit</button>` : ''}
                            <button class="btn-sm viewBtn" data-id="${r.id}">View Details</button>
                        </div>
                    </div>
                `).join('');
            }

            // Submit buttons
            document.querySelectorAll('.submitBtn').forEach(btn => {
                btn.addEventListener('click', async () => {
                    const res = await fetch(`/api/AccessRequest/${btn.dataset.id}/submit`, {
                        method: 'PATCH',
                        headers: { 'Authorization': `Bearer ${token}` }
                    });
                    if (res.ok) loadDashboard();
                });
            });

            // View buttons
            document.querySelectorAll('.viewBtn').forEach(btn => {
                btn.addEventListener('click', () => {
                    window.location.href = `request.html?id=${btn.dataset.id}`;
                });
            });

            // Load pending approvals (only for Approver and Admin)
            if (role === 'Approver' || role === 'Admin') {
                const pendingRes = await fetch('/api/AccessRequest/pending', {
                    headers: { 'Authorization': `Bearer ${token}` }
                });
                const pending = await pendingRes.json();

                if (pending.length === 0) {
                    pendingDiv.innerHTML = '<div class="empty-state">No pending approvals.</div>';
                } else {
                    pendingDiv.innerHTML = pending.map(r => `
                        <div class="request-card">
                            <div class="request-card-title">${escapeHtml(r.title)}</div>
                            <div class="request-card-meta">Resource: ${escapeHtml(r.resource)}</div>
                            <div class="request-card-meta">Status: ${statusBadge(r.status)}</div>
                            <div class="request-card-actions">
                                <button class="btn-sm approvePendingBtn" data-id="${r.id}">Approve</button>
                                <button class="btn-sm btn-danger rejectPendingBtn" data-id="${r.id}">Reject</button>
                                <button class="btn-sm viewPendingBtn" data-id="${r.id}">View</button>
                            </div>
                        </div>
                    `).join('');
                }

                document.querySelectorAll('.approvePendingBtn').forEach(btn => {
                    btn.addEventListener('click', async () => {
                        const res = await fetch(`/api/AccessRequest/${btn.dataset.id}/approve`, {
                            method: 'POST',
                            headers: { 'Authorization': `Bearer ${token}`, 'Content-Type': 'application/json' },
                            body: JSON.stringify(null)
                        });
                        if (res.ok) loadDashboard();
                    });
                });

                document.querySelectorAll('.rejectPendingBtn').forEach(btn => {
                    btn.addEventListener('click', async () => {
                        const res = await fetch(`/api/AccessRequest/${btn.dataset.id}/reject`, {
                            method: 'POST',
                            headers: { 'Authorization': `Bearer ${token}`, 'Content-Type': 'application/json' },
                            body: JSON.stringify(null)
                        });
                        if (res.ok) loadDashboard();
                    });
                });

                document.querySelectorAll('.viewPendingBtn').forEach(btn => {
                    btn.addEventListener('click', () => {
                        window.location.href = `request.html?id=${btn.dataset.id}`;
                    });
                });

            } else {
                pendingDiv.innerHTML = '<div class="empty-state">You do not have approval permissions.</div>';
            }

        } catch (err) {
            console.error('Dashboard load error:', err);
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

// =====================
// CREATE REQUEST PAGE
// =====================
if (window.location.pathname.includes('create.html')) {

    document.getElementById('requestForm').addEventListener('submit', async (e) => {
        e.preventDefault();
        const msg = document.getElementById('message');
        const payload = {
            title:         document.getElementById('title').value,
            resource:      document.getElementById('resource').value,
            accessType:    document.getElementById('accessType').value,
            justification: document.getElementById('justification').value,
            basis:         document.getElementById('basis').value || null
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
                window.location.href = 'dashboard.html';
            } else {
                msg.innerText = 'Failed to create request.';
                msg.className = 'message message-error';
            }
        } catch {
            msg.innerText = 'Network error';
            msg.className = 'message message-error';
        }
    });

    document.getElementById('cancelBtn')?.addEventListener('click', () => {
        window.location.href = 'dashboard.html';
    });
}

// =====================
// REQUEST DETAIL PAGE
// =====================
if (window.location.pathname.includes('request.html')) {

    const urlParams       = new URLSearchParams(window.location.search);
    const requestId       = urlParams.get('id');
    const requestInfo     = document.getElementById('requestInfo');
    const approvalSection = document.getElementById('approvalSection');
    const role            = getUserRole();

    async function loadRequest() {
        try {
            const res = await fetch(`/api/AccessRequest/${requestId}`, {
                headers: { 'Authorization': `Bearer ${token}` }
            });
            if (!res.ok) throw new Error('Not found');
            const req = await res.json();

            requestInfo.innerHTML = `
                <div class="detail-row">
                    <span class="detail-label">Title</span>
                    <span class="detail-value">${escapeHtml(req.title)}</span>
                </div>
                <div class="detail-row">
                    <span class="detail-label">Resource</span>
                    <span class="detail-value">${escapeHtml(req.resource)}</span>
                </div>
                <div class="detail-row">
                    <span class="detail-label">Access Type</span>
                    <span class="detail-value">${escapeHtml(req.accessType)}</span>
                </div>
                <div class="detail-row">
                    <span class="detail-label">Justification</span>
                    <span class="detail-value">${escapeHtml(req.justification)}</span>
                </div>
                <div class="detail-row">
                    <span class="detail-label">Basis</span>
                    <span class="detail-value">${escapeHtml(req.basis || '—')}</span>
                </div>
                <div class="detail-row">
                    <span class="detail-label">Status</span>
                    <span class="detail-value">${statusBadge(req.status)}</span>
                </div>
                <div class="detail-row">
                    <span class="detail-label">Created</span>
                    <span class="detail-value">${new Date(req.createdAt).toLocaleString()}</span>
                </div>
            `;

            // Show approve/reject only for correct role and correct status
            const canApprove =
                (role === 'Approver' && req.status === 'Submitted') ||
                (role === 'Admin'    && req.status === 'InApproval');

            if (canApprove) {
                approvalSection.innerHTML = `
                    <hr>
                    <div class="request-card-actions">
                        <button id="approveBtn">Approve</button>
                        <button id="rejectBtn" class="btn-danger">Reject</button>
                    </div>
                    <div class="message" id="approvalMessage"></div>
                `;

                document.getElementById('approveBtn').addEventListener('click', async () => {
                    const resp = await fetch(`/api/AccessRequest/${requestId}/approve`, {
                        method: 'POST',
                        headers: { 'Authorization': `Bearer ${token}`, 'Content-Type': 'application/json' },
                        body: JSON.stringify(null)
                    });
                    if (resp.ok) loadRequest();
                    else {
                        document.getElementById('approvalMessage').innerText = 'Approve failed';
                        document.getElementById('approvalMessage').className = 'message message-error';
                    }
                });

                document.getElementById('rejectBtn').addEventListener('click', async () => {
                    const resp = await fetch(`/api/AccessRequest/${requestId}/reject`, {
                        method: 'POST',
                        headers: { 'Authorization': `Bearer ${token}`, 'Content-Type': 'application/json' },
                        body: JSON.stringify(null)
                    });
                    if (resp.ok) loadRequest();
                    else {
                        document.getElementById('approvalMessage').innerText = 'Reject failed';
                        document.getElementById('approvalMessage').className = 'message message-error';
                    }
                });
            } else {
                approvalSection.innerHTML = '';
            }

        } catch (err) {
            requestInfo.innerHTML = '<div class="empty-state">Request not found.</div>';
        }
    }

    document.getElementById('backBtn')?.addEventListener('click', () => {
        window.location.href = 'dashboard.html';
    });

    if (requestId) loadRequest();
}