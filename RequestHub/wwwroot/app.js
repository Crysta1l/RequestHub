// =====================
// SHARED UTILITIES
// =====================

const token = localStorage.getItem('token');

if (!token && !window.location.pathname.includes('login.html') && !window.location.pathname.includes('index.html')) {
    window.location.href = 'login.html';
}

function escapeHtml(str) {
    if (!str) return '';
    return str.replace(/[&<>]/g, m => (m === '&' ? '&amp;' : m === '<' ? '&lt;' : '&gt;'));
}

function getUserRole() {
    if (!token) return '';
    try {
        const payload = JSON.parse(atob(token.split('.')[1]));
        return payload['http://schemas.microsoft.com/ws/2008/06/identity/claims/role'] || '';
    } catch {
        return '';
    }
}

function statusBadge(status) {
    const map = {
        'Draft': 'badge-draft',
        'Submitted': 'badge-submitted',
        'InApproval': 'badge-inapproval',
        'Approved': 'badge-approved',
        'Rejected': 'badge-rejected',
    };
    const cls = map[status] || 'badge-draft';
    return `<span class="badge ${cls}">${escapeHtml(status)}</span>`;
}

function priorityBadge(priority) {
    const map = {
        'Low': 'priority-low',
        'Medium': 'priority-medium',
        'High': 'priority-high',
    };
    const cls = map[priority] || 'priority-medium';
    return `<span class="${cls}">${escapeHtml(priority)}</span>`;
}

// =====================
// LOGIN PAGE
// =====================
if (window.location.pathname.includes('login.html') || window.location.pathname.includes('index.html') || window.location.pathname === '/') {

    const msg = document.getElementById('authMessage');

    function getCredentials() {
        return {
            email: document.getElementById('email').value,
            password: document.getElementById('password').value
        };
    }

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
    const pendingDiv = document.getElementById('pendingList');
    const statsBar = document.getElementById('statsBar');
    const role = getUserRole();

    async function loadStats(requests) {
        const total = requests.length;
        const draft = requests.filter(r => r.status === 'Draft').length;
        const pending = requests.filter(r => r.status === 'Submitted' || r.status === 'InApproval').length;
        const approved = requests.filter(r => r.status === 'Approved').length;
        const rejected = requests.filter(r => r.status === 'Rejected').length;

        statsBar.innerHTML = `
            <div class="stat-card"><span>${total}</span>Total</div>
            <div class="stat-card"><span>${draft}</span>Draft</div>
            <div class="stat-card"><span>${pending}</span>Pending</div>
            <div class="stat-card"><span>${approved}</span>Approved</div>
            <div class="stat-card"><span>${rejected}</span>Rejected</div>
        `;
    }

    async function loadDashboard() {
        try {
            const status = document.getElementById('filterStatus').value;
            const resource = document.getElementById('filterResource').value;
            const title = document.getElementById('filterTitle').value;

            let url = '/api/AccessRequest?';
            if (status) url += `status=${encodeURIComponent(status)}&`;
            if (resource) url += `resource=${encodeURIComponent(resource)}&`;
            if (title) url += `title=${encodeURIComponent(title)}`;

            const myRes = await fetch(url, { headers: { 'Authorization': `Bearer ${token}` } });
            const myRequests = await myRes.json();

            loadStats(myRequests);

            if (myRequests.length === 0) {
                myRequestsDiv.innerHTML = '<div class="empty-state">No requests found.</div>';
            } else {
                myRequestsDiv.innerHTML = myRequests.map(r => `
                    <div class="request-card">
                        <div class="request-card-title">${escapeHtml(r.title)}</div>
                        <div class="request-card-meta">Resource: ${escapeHtml(r.resource)}</div>
                        <div class="request-card-meta">Access: ${escapeHtml(r.accessType)}</div>
                        <div class="request-card-meta">Priority: ${priorityBadge(r.priority)}</div>
                        <div class="request-card-meta">Status: ${statusBadge(r.status)}</div>
                        ${r.status === 'Approved' ? `<div class="request-card-meta">Acknowledged: ${r.isAcknowledged ? '✓ Yes' : '✗ No'}</div>` : ''}
                        <div class="request-card-actions">
                            ${r.status === 'Draft' ? `<button class="btn-sm submitBtn" data-id="${r.id}">Submit</button>` : ''}
                            <button class="btn-sm viewBtn" data-id="${r.id}">View Details</button>
                        </div>
                    </div>
                `).join('');
            }

            document.querySelectorAll('.submitBtn').forEach(btn => {
                btn.addEventListener('click', async () => {
                    const res = await fetch(`/api/AccessRequest/${btn.dataset.id}/submit`, {
                        method: 'PATCH',
                        headers: { 'Authorization': `Bearer ${token}` }
                    });
                    if (res.ok) loadDashboard();
                });
            });

            document.querySelectorAll('.viewBtn').forEach(btn => {
                btn.addEventListener('click', () => {
                    window.location.href = `request.html?id=${btn.dataset.id}`;
                });
            });

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
                            <div class="request-card-meta">Priority: ${priorityBadge(r.priority)}</div>
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
                        const comment = prompt('Comment (optional):');
                        const res = await fetch(`/api/AccessRequest/${btn.dataset.id}/approve`, {
                            method: 'POST',
                            headers: { 'Authorization': `Bearer ${token}`, 'Content-Type': 'application/json' },
                            body: JSON.stringify({ comment: comment || null })
                        });
                        if (res.ok) loadDashboard();
                    });
                });

                document.querySelectorAll('.rejectPendingBtn').forEach(btn => {
                    btn.addEventListener('click', async () => {
                        const comment = prompt('Reason for rejection (optional):');
                        const res = await fetch(`/api/AccessRequest/${btn.dataset.id}/reject`, {
                            method: 'POST',
                            headers: { 'Authorization': `Bearer ${token}`, 'Content-Type': 'application/json' },
                            body: JSON.stringify({ comment: comment || null })
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

    document.getElementById('reportBtn')?.addEventListener('click', async () => {
        const res = await fetch('/api/AccessRequest/report', {
            headers: { 'Authorization': `Bearer ${token}` }
        });
        if (res.ok) {
            const blob = await res.blob();
            const url = URL.createObjectURL(blob);
            const a = document.createElement('a');
            a.href = url;
            a.download = 'requests.csv';
            a.click();
            URL.revokeObjectURL(url);
        }
    });

    document.getElementById('logoutBtn')?.addEventListener('click', () => {
        localStorage.removeItem('token');
        window.location.href = 'login.html';
    });

    document.getElementById('filterStatus')?.addEventListener('change', loadDashboard);
    document.getElementById('filterResource')?.addEventListener('input', loadDashboard);
    document.getElementById('filterTitle')?.addEventListener('input', loadDashboard);

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
            title: document.getElementById('title').value,
            resource: document.getElementById('resource').value,
            accessType: document.getElementById('accessType').value,
            justification: document.getElementById('justification').value,
            priority: document.getElementById('priority').value,
            department: document.getElementById('department').value || null,
            expiryDate: document.getElementById('expiryDate').value || null,
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

    const urlParams = new URLSearchParams(window.location.search);
    const requestId = urlParams.get('id');
    const requestInfo = document.getElementById('requestInfo');
    const approvalSection = document.getElementById('approvalSection');
    const historyList = document.getElementById('historyList');
    const role = getUserRole();

    async function loadFiles() {
        try {
            const res = await fetch(`/api/File/${requestId}/files`, {
                headers: { 'Authorization': `Bearer ${token}` }
            });
            if (!res.ok) return;
            const files = await res.json();
            const filesList = document.getElementById('filesList');

            if (files.length === 0) {
                filesList.innerHTML = '<div class="empty-state">No files attached.</div>';
            } else {
                filesList.innerHTML = files.map(f => `
                    <div class="request-card">
                        <div class="request-card-title">${escapeHtml(f.fileName)}</div>
                        <div class="request-card-meta">${(f.fileSize / 1024).toFixed(1)} KB · ${new Date(f.uploadedAt).toLocaleString()}</div>
                        <div class="request-card-actions">
                            <button class="btn-sm downloadFileBtn" data-id="${f.id}" data-name="${escapeHtml(f.fileName)}">Download</button>
                            <button class="btn-sm btn-danger deleteFileBtn" data-id="${f.id}">Delete</button>
                        </div>
                    </div>
                `).join('');
            }

            document.querySelectorAll('.downloadFileBtn').forEach(btn => {
                btn.addEventListener('click', async () => {
                    const res = await fetch(`/api/File/download/${btn.dataset.id}`, {
                        headers: { 'Authorization': `Bearer ${token}` }
                    });
                    if (res.ok) {
                        const blob = await res.blob();
                        const url = URL.createObjectURL(blob);
                        const a = document.createElement('a');
                        a.href = url;
                        a.download = btn.dataset.name;
                        a.click();
                        URL.revokeObjectURL(url);
                    }
                });
            });

            document.querySelectorAll('.deleteFileBtn').forEach(btn => {
                btn.addEventListener('click', async () => {
                    const res = await fetch(`/api/File/${btn.dataset.id}`, {
                        method: 'DELETE',
                        headers: { 'Authorization': `Bearer ${token}` }
                    });
                    if (res.ok) loadFiles();
                });
            });

        } catch (err) {
            console.error('Files load error:', err);
        }
    }

    async function loadHistory() {
        try {
            const res = await fetch(`/api/AccessRequest/${requestId}/history`, {
                headers: { 'Authorization': `Bearer ${token}` }
            });
            if (!res.ok) return;
            const history = await res.json();

            if (history.length === 0) {
                historyList.innerHTML = '<div class="empty-state">No history yet.</div>';
            } else {
                historyList.innerHTML = history.map(h => `
                    <div class="request-card">
                        <div class="request-card-meta">${new Date(h.performedAt).toLocaleString()}</div>
                        <div class="request-card-title">${escapeHtml(h.action)}</div>
                        ${h.oldStatus ? `<div class="request-card-meta">${escapeHtml(h.oldStatus)} → ${escapeHtml(h.newStatus)}</div>` : ''}
                        ${h.ipAddress ? `<div class="request-card-meta">IP: ${escapeHtml(h.ipAddress)}</div>` : ''}
                    </div>
                `).join('');
            }
        } catch (err) {
            console.error('History load error:', err);
        }
    }

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
                    <span class="detail-label">Priority</span>
                    <span class="detail-value">${priorityBadge(req.priority)}</span>
                </div>
                <div class="detail-row">
                    <span class="detail-label">Department</span>
                    <span class="detail-value">${escapeHtml(req.department || '—')}</span>
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
                    <span class="detail-label">Expiry Date</span>
                    <span class="detail-value">${req.expiryDate ? new Date(req.expiryDate).toLocaleDateString() : '—'}</span>
                </div>
                <div class="detail-row">
                    <span class="detail-label">Status</span>
                    <span class="detail-value">${statusBadge(req.status)}</span>
                </div>
                <div class="detail-row">
                    <span class="detail-label">Acknowledged</span>
                    <span class="detail-value">${req.isAcknowledged ? '✓ Yes — ' + new Date(req.acknowledgedAt).toLocaleString() : '✗ No'}</span>
                </div>
                <div class="detail-row">
                    <span class="detail-label">Created</span>
                    <span class="detail-value">${new Date(req.createdAt).toLocaleString()}</span>
                </div>
            `;

            const canApprove =
                (role === 'Approver' && req.status === 'Submitted') ||
                (role === 'Admin' && req.status === 'InApproval');

            if (canApprove) {
                approvalSection.innerHTML = `
                    <hr>
                    <div class="form-group">
                        <label for="commentInput">Comment (optional)</label>
                        <textarea id="commentInput" rows="2" placeholder="Add a comment..."></textarea>
                    </div>
                    <div class="request-card-actions">
                        <button id="approveBtn">Approve</button>
                        <button id="rejectBtn" class="btn-danger">Reject</button>
                    </div>
                    <div class="message" id="approvalMessage"></div>
                `;

                document.getElementById('approveBtn').addEventListener('click', async () => {
                    const comment = document.getElementById('commentInput').value || null;
                    const resp = await fetch(`/api/AccessRequest/${requestId}/approve`, {
                        method: 'POST',
                        headers: { 'Authorization': `Bearer ${token}`, 'Content-Type': 'application/json' },
                        body: JSON.stringify({ comment })
                    });
                    if (resp.ok) { loadRequest(); loadHistory(); }
                    else {
                        document.getElementById('approvalMessage').innerText = 'Approve failed';
                        document.getElementById('approvalMessage').className = 'message message-error';
                    }
                });

                document.getElementById('rejectBtn').addEventListener('click', async () => {
                    const comment = document.getElementById('commentInput').value || null;
                    const resp = await fetch(`/api/AccessRequest/${requestId}/reject`, {
                        method: 'POST',
                        headers: { 'Authorization': `Bearer ${token}`, 'Content-Type': 'application/json' },
                        body: JSON.stringify({ comment })
                    });
                    if (resp.ok) { loadRequest(); loadHistory(); }
                    else {
                        document.getElementById('approvalMessage').innerText = 'Reject failed';
                        document.getElementById('approvalMessage').className = 'message message-error';
                    }
                });

            } else if (req.status === 'Approved' && !req.isAcknowledged && role === 'Requester') {
                approvalSection.innerHTML = `
                    <hr>
                    <p style="color: var(--text-secondary); margin-bottom: 1rem;">Please confirm that you have received access.</p>
                    <button id="acknowledgeBtn">✓ I Acknowledge Access</button>
                    <div class="message" id="approvalMessage"></div>
                `;

                document.getElementById('acknowledgeBtn').addEventListener('click', async () => {
                    const resp = await fetch(`/api/AccessRequest/${requestId}/acknowledge`, {
                        method: 'PATCH',
                        headers: { 'Authorization': `Bearer ${token}` }
                    });
                    if (resp.ok) { loadRequest(); loadHistory(); }
                });

            } else if (req.status === 'Approved' && req.isAcknowledged) {
                approvalSection.innerHTML = `
                    <hr>
                    <p style="color: #70d090;">✓ Access acknowledged on ${new Date(req.acknowledgedAt).toLocaleString()}</p>
                `;
            } else {
                approvalSection.innerHTML = '';
            }

            loadHistory();
            loadFiles();

        } catch (err) {
            requestInfo.innerHTML = '<div class="empty-state">Request not found.</div>';
        }
    }

    document.getElementById('uploadBtn')?.addEventListener('click', async () => {
        const fileInput = document.getElementById('fileInput');
        if (!fileInput.files[0]) return;

        const formData = new FormData();
        formData.append('file', fileInput.files[0]);

        const res = await fetch(`/api/File/${requestId}/upload`, {
            method: 'POST',
            headers: { 'Authorization': `Bearer ${token}` },
            body: formData
        });

        if (res.ok) {
            fileInput.value = '';
            loadFiles();
        } else {
            document.getElementById('message').innerText = 'Upload failed';
            document.getElementById('message').className = 'message message-error';
        }
    });

    document.getElementById('backBtn')?.addEventListener('click', () => {
        window.location.href = 'dashboard.html';
    });

    if (requestId) loadRequest();
}