using System.Collections;
using System.Collections.Generic;
using Assets.Source.Flow;

namespace KingdomFoundation.Api
{
    internal sealed class WorkspacePortList : IReadOnlyList<WorkspacePort>
    {
        private readonly FlowElement element;
        private readonly bool inputs;
        internal WorkspacePortList(FlowElement element,bool inputs){this.element=element;this.inputs=inputs;}
        public int Count=>inputs ? element.Inputs.Count : element.Outputs.Count;
        public WorkspacePort this[int index]=>WorkspacePort.Wrap(inputs ? (FlowIO)element.Inputs[index] : element.Outputs[index]);
        public IEnumerator<WorkspacePort> GetEnumerator(){for(int i=0;i<Count;i++)yield return this[i];}
        IEnumerator IEnumerable.GetEnumerator()=>GetEnumerator();
    }
}
