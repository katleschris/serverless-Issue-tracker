import { useState, useEffect, ReactElement } from 'react';
import Head from 'next/head';
import { Plus, Filter, RefreshCw, AlertCircle } from 'lucide-react';
import { issueApi } from '@/lib/api';
import IssueCard from '@/components/IssueCard';
import CreateIssueForm from '@/components/CreateIssueForm';

type IssueStatus = 'Open' | 'InProgress' | 'Done' | 'All';

interface Issue {
  id: string;
  title: string;
  description: string;
  status: 'Open' | 'InProgress' | 'Done';
  priority: 'Low' | 'Medium' | 'High';
  createdAt: string;
}

interface ApiResponse<T> {
  success: boolean;
  data?: T;
  error?: {
    message: string;
  };
}

interface FormData {
  title: string;
  description: string;
  priority: 'Low' | 'Medium' | 'High';
}

interface Stats {
  total: number;
  open: number;
  inProgress: number;
  done: number;
}

export default function Home(): ReactElement {
  const [issues, setIssues] = useState<Issue[]>([]);
  const [loading, setLoading] = useState<boolean>(true);
  const [error, setError] = useState<string | null>(null);
  const [statusFilter, setStatusFilter] = useState<IssueStatus>('All');
  const [showCreateForm, setShowCreateForm] = useState<boolean>(false);
  const [successMessage, setSuccessMessage] = useState<string>('');

  // Load issues on mount and when filter changes
  useEffect(() => {
    loadIssues();
  }, [statusFilter]);

 const loadIssues = async (): Promise<void> => {
  try {
    setLoading(true);
    setError(null);

    const filterValue = statusFilter === 'All' ? undefined : statusFilter;
    const response = await issueApi.getAllIssues(filterValue);

    if (response.success) {
      setIssues(response.data || []);
    } else {
      setError(response.error?.message || 'Failed to load issues');
    }
  } catch (err) {
    console.error('Error loading issues:', err);
    setError('Failed to connect to the API. Make sure the backend is deployed.');
  } finally {
    setLoading(false);
  }
};

  const handleCreateIssue = async (issueData: FormData): Promise<void> => {
    try {
      const response = await issueApi.createIssue(issueData);

      if (response.success) {
        setShowCreateForm(false);
        showSuccess('Issue created successfully!');
        loadIssues(); // Reload list
      } else {
        alert(response.error?.message || 'Failed to create issue');
      }
    } catch (err) {
      console.error('Error creating issue:', err);
      alert('Failed to create issue');
    }
  };

  const handleStatusChange = async (id: string, newStatus: string): Promise<void> => {
    try {
      const response = await issueApi.updateIssue(id, { status: newStatus });

      if (response.success) {
        showSuccess('Status updated!');
        loadIssues(); // Reload to reflect changes
      } else {
        alert(response.error?.message || 'Failed to update status');
      }
    } catch (err) {
      console.error('Error updating status:', err);
      alert('Failed to update status');
    }
  };

  const handleDeleteIssue = async (id: string): Promise<void> => {
    if (!window.confirm('Are you sure you want to delete this issue?')) {
      return;
    }

    try {
      const response = await issueApi.deleteIssue(id);

      if (response.success) {
        showSuccess('Issue deleted!');
        loadIssues(); // Reload list
      } else {
        alert(response.error?.message || 'Failed to delete issue');
      }
    } catch (err) {
      console.error('Error deleting issue:', err);
      alert('Failed to delete issue');
    }
  };

  const showSuccess = (message: string): void => {
    setSuccessMessage(message);
    setTimeout(() => setSuccessMessage(''), 3000);
  };

  const stats: Stats = {
    total: issues.length,
    open: issues.filter((i) => i.status === 'Open').length,
    inProgress: issues.filter((i) => i.status === 'InProgress').length,
    done: issues.filter((i) => i.status === 'Done').length,
  };

  return (
    <>
      <Head>
        <title>Issue Tracker - Manage Your Project Issues</title>
        <meta name="description" content="Serverless issue tracking application" />
        <meta name="viewport" content="width=device-width, initial-scale=1" />
        <link rel="icon" href="/favicon.ico" />
      </Head>

      <div className="min-h-screen bg-gray-50">
        {/* Header */}
        <header className="bg-white shadow-sm">
          <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 py-6">
            <div className="flex justify-between items-center">
              <div>
                <h1 className="text-3xl font-bold text-gray-900">Issue Tracker</h1>
                <p className="text-gray-600 mt-1">
                  Manage and track your project issues
                </p>
              </div>
              <button
                onClick={() => setShowCreateForm(true)}
                className="flex items-center gap-2 px-6 py-3 bg-blue-600 text-white rounded-lg hover:bg-blue-700 transition-colors shadow-md"
              >
                <Plus size={20} />
                New Issue
              </button>
            </div>

            {/* Stats */}
            <div className="grid grid-cols-2 md:grid-cols-4 gap-4 mt-6">
              <div className="bg-gray-100 rounded-lg p-4">
                <div className="text-2xl font-bold text-gray-900">{stats.total}</div>
                <div className="text-sm text-gray-600">Total Issues</div>
              </div>
              <div className="bg-blue-100 rounded-lg p-4">
                <div className="text-2xl font-bold text-blue-900">{stats.open}</div>
                <div className="text-sm text-blue-700">Open</div>
              </div>
              <div className="bg-yellow-100 rounded-lg p-4">
                <div className="text-2xl font-bold text-yellow-900">{stats.inProgress}</div>
                <div className="text-sm text-yellow-700">In Progress</div>
              </div>
              <div className="bg-green-100 rounded-lg p-4">
                <div className="text-2xl font-bold text-green-900">{stats.done}</div>
                <div className="text-sm text-green-700">Done</div>
              </div>
            </div>
          </div>
        </header>

        {/* Main Content */}
        <main className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 py-8">
          {/* Filters & Actions */}
          <div className="flex flex-col sm:flex-row justify-between items-start sm:items-center gap-4 mb-6">
            <div className="flex items-center gap-2">
              <Filter size={20} className="text-gray-600" />
              <select
                value={statusFilter}
                onChange={(e) => setStatusFilter(e.target.value as IssueStatus)}
                className="px-4 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-blue-500 focus:border-transparent"
              >
                <option value="All">All Status</option>
                <option value="Open">Open</option>
                <option value="InProgress">In Progress</option>
                <option value="Done">Done</option>
              </select>
            </div>

            <button
              onClick={loadIssues}
              className="flex items-center gap-2 px-4 py-2 text-gray-700 hover:bg-gray-100 rounded-lg transition-colors"
            >
              <RefreshCw size={18} />
              Refresh
            </button>
          </div>

          {/* Success Message */}
          {successMessage && (
            <div className="mb-6 bg-green-100 border border-green-400 text-green-700 px-4 py-3 rounded-lg flex items-center gap-2">
              <AlertCircle size={20} />
              {successMessage}
            </div>
          )}

          {/* Error State */}
          {error && (
            <div className="bg-red-100 border border-red-400 text-red-700 px-4 py-3 rounded-lg mb-6">
              <div className="flex items-start gap-2">
                <AlertCircle size={20} className="mt-0.5 flex-shrink-0" />
                <div>
                  <p className="font-medium">Error loading issues</p>
                  <p className="text-sm mt-1">{error}</p>
                  <p className="text-sm mt-2">
                    Make sure you've deployed the backend and set{' '}
                    <code className="bg-red-200 px-1 rounded">NEXT_PUBLIC_API_URL</code>{' '}
                    in your <code className="bg-red-200 px-1 rounded">.env.local</code> file
                  </p>
                </div>
              </div>
            </div>
          )}

          {/* Loading State */}
          {loading && (
            <div className="flex justify-center items-center py-12">
              <div className="animate-spin rounded-full h-12 w-12 border-b-2 border-blue-600"></div>
            </div>
          )}

          {/* Empty State */}
          {!loading && !error && issues.length === 0 && (
            <div className="text-center py-12">
              <div className="text-gray-400 mb-4">
                <AlertCircle size={48} className="mx-auto" />
              </div>
              <h3 className="text-lg font-medium text-gray-900 mb-2">No issues found</h3>
              <p className="text-gray-600 mb-4">
                {statusFilter === 'All'
                  ? "Get started by creating your first issue!"
                  : `No issues with status "${statusFilter}"`}
              </p>
              <button
                onClick={() => setShowCreateForm(true)}
                className="inline-flex items-center gap-2 px-6 py-3 bg-blue-600 text-white rounded-lg hover:bg-blue-700 transition-colors"
              >
                <Plus size={20} />
                Create First Issue
              </button>
            </div>
          )}

          {/* Issues Grid */}
          {!loading && !error && issues.length > 0 && (
            <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-6">
              {issues.map((issue) => (
                <IssueCard
                  key={issue.id}
                  issue={issue}
                  onStatusChange={handleStatusChange}
                  onDelete={handleDeleteIssue}
                />
              ))}
            </div>
          )}
        </main>

        {/* Create Form Modal */}
        {showCreateForm && (
          <CreateIssueForm
            onSubmit={handleCreateIssue}
            onCancel={() => setShowCreateForm(false)}
          />
        )}
      </div>
    </>
  );
}