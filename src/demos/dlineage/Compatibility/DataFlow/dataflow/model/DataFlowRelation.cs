namespace gudusoft.gsqlparser.demos.dataFlowAnalyzer.dataflow.model
{


	public class DataFlowRelation : AbstractRelation
	{

		public override RelationType RelationType
		{
			get
			{
				return RelationType.dataflow;
			}
		}
	}

}