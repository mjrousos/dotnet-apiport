﻿// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace PortabilityService.WorkflowManagement
{
    public enum WorkflowStage
    {
        Analyze,
        Report,
        Telemetry,
        Finished
    }

    /// <summary>
    /// Workflow stages are in the following order: Analyze -> Report -> Telemetry -> Finished
    /// </summary>
    public class WorkflowManager
    {
        readonly IWorkflowAction[] actions;
        static WorkflowManager manager;

        /// <returns></returns>
        public static WorkflowManager Initialize(IWorkflowAction[] workflowActions)
        {
            if (manager == null)
            {
                manager = new WorkflowManager(workflowActions);
            }

            return manager;
        }

        public static WorkflowManager Initialize()
        {
            if (manager == null)
            {
                manager = new WorkflowManager();
            }

            return manager;
        }

        private WorkflowManager()
        {
            //TODO: When DI is implemented, change this to use that
            actions = new IWorkflowAction[Enum.GetValues(typeof(WorkflowStage)).Length-1];

            AddAction<AnalyzeAction>();
            AddAction<ReportAction>();
            AddAction<TelemetryAction>();
        }

        private WorkflowManager(IWorkflowAction[] workflowActions)
        {
            actions = workflowActions;
        }

        private void AddAction<T>() where T : IWorkflowAction, new()
        {
            T action = new T();
            actions[(int)action.CurrentStage] = action;
        }

        /// <summary>
        /// Gets the first message used to start a workflow for a submission.
        /// </summary>
        public static WorkflowQueueMessage GetFirstStage(string submissionId)
        {
            return new WorkflowQueueMessage(submissionId, WorkflowStage.Analyze);
        }

        /// <summary>
        /// Executes the action for the current stage in the workflow. After the action completes,
        /// returns a message for the next stage in the queue. 
        /// </summary>
        public async Task<WorkflowQueueMessage> ExecuteActionsToNextStage(WorkflowQueueMessage currentMsg, CancellationToken cancelToken)
        {
            //Get action corresponding to the current message's workflow stage
            Debug.Assert(actions.Length - 1 > (int)currentMsg.Stage, "Stage must be within bounds of Actions array.");
            var action = actions[(int)currentMsg.Stage];
            Debug.Assert(action.CurrentStage == currentMsg.Stage, "Action's Stage must match current message's Stage.");

            //Execute the action
            WorkflowStage nextStage = await action.ExecuteAsync(currentMsg.SubmissionId, cancelToken).ConfigureAwait(false);

            return new WorkflowQueueMessage(currentMsg.SubmissionId, nextStage);
        }
    }
}
