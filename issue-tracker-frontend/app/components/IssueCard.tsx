import React from 'react';
import { Trash2, Clock, AlertCircle } from 'lucide-react';

const IssueCard = ({ issue, onStatusChange, onDelete }) => {
  const getStatusColor = (status) => {
    switch (status) {
      case 'Open':
        return 'bg-blue-100 text-blue-800 border-blue-200';
      case 'InProgress':
        return 'bg-yellow-100 text-yellow-800 border-yellow-200';
      case 'Done':
        return 'bg-green-100 text-green-800 border-green-200';
      default:
        return 'bg-gray-100 text-gray-800 border-gray-200';
    }
  };

  const getPriorityColor = (priority) => {
    switch (priority) {
      case 'High':
        return 'text-red-600';
      case 'Medium':
        return 'text-orange-600';
      case 'Low':
        return 'text-blue-600';
      default:
        return 'text-gray-600';
    }
  };

  const formatDate = (dateString) => {
    return new Date(dateString).toLocaleDateString('en-US', {
      month: 'short',
      day: 'numeric',
      year: 'numeric',
      hour: '2-digit',
      minute: '2-digit',
    });
  };

  return (
    <div className="bg-white rounded-lg shadow-md p-6 hover:shadow-lg transition-shadow">
      {/* Header */}
      <div className="flex justify-between items-start mb-3">
        <h3 className="text-lg font-semibold text-gray-900 flex-1">
          {issue.title}
        </h3>
        <button
          onClick={() => onDelete(issue.id)}
          className="text-red-600 hover:text-red-800 transition-colors p-1"
          title="Delete issue"
        >
          <Trash2 size={18} />
        </button>
      </div>

      {/* Description */}
      <p className="text-gray-600 mb-4 line-clamp-2">{issue.description}</p>

      {/* Status & Priority */}
      <div className="flex gap-2 mb-4">
        <select
          value={issue.status}
          onChange={(e) => onStatusChange(issue.id, e.target.value)}
          className={`px-3 py-1 rounded-full text-sm font-medium border ${getStatusColor(
            issue.status
          )} cursor-pointer hover:opacity-80 transition-opacity`}
        >
          <option value="Open">Open</option>
          <option value="InProgress">In Progress</option>
          <option value="Done">Done</option>
        </select>

        <span className={`flex items-center gap-1 ${getPriorityColor(issue.priority)}`}>
          <AlertCircle size={16} />
          <span className="text-sm font-medium">{issue.priority}</span>
        </span>
      </div>

      {/* Timestamps */}
      <div className="flex items-center gap-4 text-xs text-gray-500">
        <div className="flex items-center gap-1">
          <Clock size={14} />
          <span>Created: {formatDate(issue.createdAt)}</span>
        </div>
      </div>
    </div>
  );
};

export default IssueCard;