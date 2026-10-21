namespace gudusoft.gsqlparser.demos.dataFlowAnalyzer.dataflow.model
{

	public interface Relation
	{
		RelationElement Target {get;}

		RelationElement[] Sources {get;}

		RelationType RelationType {get;}
	}

}