import { json } from '@sveltejs/kit';
import { hash } from '@node-rs/argon2';
import { prisma } from '$lib/server/db.js';
import type { RequestHandler } from './$types';
import { createSession } from '$lib/server/session.js';

const EMAIL_RE = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;
const NAME_RE = /^[A-Za-z0-9_-]{3,20}$/; // no '@', so a name can never look like an email

export const POST: RequestHandler = async ({ request, cookies }) => {
    let body: { email?: unknown; password?: unknown; display_name?: unknown };
    try {
        body = await request.json();
    } catch {
        return json({ success: false, error: 'Invalid JSON' }, { status: 400 });
    }

    const { email, password, display_name } = body;
    if (typeof email !== 'string' || typeof password !== 'string'
        || typeof display_name !== 'string') {
        return json({ success: false, error: 'Missing required fields' }, { status: 400 });
    }

    const cleanEmail = email.trim().toLowerCase();
    const cleanName = display_name.trim();

    if (cleanEmail.length > 254 || !EMAIL_RE.test(cleanEmail)) {
        return json({ success: false, error: 'Invalid email address' }, { status: 400 });
    }
    if (!NAME_RE.test(cleanName)) {
        return json(
            { success: false, error: 'Display name must be 3-20 characters: letters, digits, _ or -' },
            { status: 400 }
        );
    }
    if (password.length < 8 || password.length > 128) {
        return json(
            { success: false, error: 'Password must be between 8 and 128 characters' },
            { status: 400 }
        );
    }

    try {
        const password_hash = await hash(password);

        const user = await prisma.user.create({
            data: { email: cleanEmail, passwordHash: password_hash, displayName: cleanName },
            select: { id: true, email: true, displayName: true, createdAt: true }
        });

        await createSession(user.id, cookies);
        return json({
            success: true,
            user: {
                id: user.id,
                email: user.email,
                display_name: user.displayName,
                created_at: user.createdAt
            }
        }, { status: 201 });
    } catch (error) {
        const e = error as { code?: string; meta?: { target?: unknown } };

        if (e.code === 'P2002') {
            const target = JSON.stringify(e.meta?.target ?? '').toLowerCase();
            const field = target.includes('displayname') || target.includes('display_name')
                ? 'display name'
                : 'email';
            return json(
                { success: false, error: `This ${field} is already taken` },
                { status: 409 }
            );
        }

        console.error('Registration failed:', error);
        return json({ success: false, error: 'Registration failed' }, { status: 500 });
    }
};
